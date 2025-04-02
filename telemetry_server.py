import mmap
import json
import struct
import time
import queue
from pathlib import Path
from typing import Dict, Any, cast
from dataclasses import dataclass
from multiprocessing import Queue, Process
from threading import Thread

# =============================================
# DATA STRUCTURE DEFINITIONS (for autocomplete)
# =============================================
#region
@dataclass
class TyreTemps:
    front_left: float
    front_right: float
    rear_left: float
    rear_right: float

@dataclass
class TyreWear:
    front_left: float
    front_right: float
    rear_left: float
    rear_right: float

@dataclass
class Tyres:
    compound: str
    temps: TyreTemps
    wear: TyreWear

@dataclass
class Engine:
    temp: float
    deg: float

@dataclass
class ERS:
    deg: float

@dataclass
class Gearbox:
    deg: float

@dataclass
class Components:
    engine: Engine
    ers: ERS
    gearbox: Gearbox

@dataclass
class Modes:
    pace: str
    fuel: str
    ers: str

@dataclass
class LastSectorTimes:
    s1: float
    s2: float
    s3: float

@dataclass
class SectorTimes:
    last: LastSectorTimes

@dataclass
class LapTime:
    current: float
    last: float
    best: float
    sector: SectorTimes

@dataclass
class Lap:
    current: int
    position: int
    time: LapTime

@dataclass
class Car:
    driverNumber: int
    speed: float
    rpm: float
    gear: int
    charge: float
    fuel: float
    lap: Lap
    tyres: Tyres
    modes: Modes
    components: Components

@dataclass
class Track:
    rubber: float
    temp: float

@dataclass
class Weather:
    air_temp: float

@dataclass
class Session:
    time_elapsed: float
    best_time: float
    track: Track
    weather: Weather

@dataclass
class Telemetry:
    car: Car
    session: Session
#endregion

class TelemetryData:
    def __init__(self, data_dict):
        # Convert nested dictionaries to dataclasses
        self.telemetry = self._convert_to_dataclass(data_dict.get('telemetry', {}), Telemetry)
    
    def _convert_to_dataclass(self, data, dataclass_type):
        if not isinstance(data, dict):
            return data
            
        # Get the field types from the dataclass
        fields = dataclass_type.__dataclass_fields__
        
        # Prepare arguments for the dataclass
        kwargs = {}
        for field_name, field_info in fields.items():
            field_value = data.get(field_name)
            
            # Get the field's type annotation
            field_type = field_info.type
            
            # Handle nested dataclasses
            if hasattr(field_type, '__dataclass_fields__'):
                kwargs[field_name] = self._convert_to_dataclass(field_value, field_type)
            else:
                kwargs[field_name] = field_value
                
        return dataclass_type(**kwargs)

# =============================================
# RECEIVER CLASS
# =============================================
class TelemetryReceiver:
    def __init__(self, export_queue: Queue = None, plot_queue: Queue = None):
        self.mmf = None
        self.file = None
        self.export_queue = export_queue  # Queue for CSV exporter
        self.plot_queue = plot_queue      # Queue for live plotter
        self.running = False
        self.last_data_time = time.time()

    def connect(self):
        try:
            while not Path('F1Manager_Telemetry').exists():
                print("Waiting for shared memory file...")
                time.sleep(1)
            
            self.file = open('F1Manager_Telemetry', 'r+b')
            self.mmf = mmap.mmap(self.file.fileno(), 0)
            print("Connected to telemetry data!")
            
            self.running = True
            Thread(target=self._broadcast_loop, daemon=True).start()
            return True
        except Exception as e:
            print(f"Connection error: {str(e)}")
            return False

    def _broadcast_loop(self):
        """Optimized data broadcasting with queue priority"""
        while self.running:
            try:
                data = self._read_mmap()
                if data:
                    # Always send to export queue first
                    if self.export_queue:
                        try:
                            self.export_queue.put_nowait(data)  # Non-blocking
                        except queue.Full:
                            print("Export queue full - dropping data")

                    # Then send to plot queue if there's capacity
                    if self.plot_queue and not self.plot_queue.full():
                        try:
                            self.plot_queue.put_nowait(data)
                        except queue.Full:
                            pass
            except Exception as e:
                print(f"Broadcast error: {e}")
                time.sleep(0.1)

    def _read_mmap(self):
        """Internal method to read mmap (replaces get_telemetry())."""
        try:
            self.mmf.seek(0)
            length_bytes = self.mmf.read(4)
            if len(length_bytes) != 4:
                return None
            
            length = struct.unpack("<I", length_bytes)[0]
            json_data = self.mmf.read(length).decode('utf-8')
            raw_data = json.loads(json_data)
            
            processed = {}
            for car_name, car_data in raw_data.items():
                processed[car_name] = TelemetryData(car_data)
            
            return processed
        except Exception as e:
            print(f"Read error: {str(e)}")
            return None

    def close(self):
        self.running = False
        if self.mmf:
            self.mmf.close()
        if self.file:
            self.file.close()

# =============================================
# MAIN PROGRAM
# =============================================
if __name__ == "__main__":
    print("F1 Manager Data Server")
    
    receiver = TelemetryReceiver()
    if not receiver.connect():
        exit(1)

    try:
        while True:
            if not receiver.running:
                    print("DATA SERVER ERROR: Broadcast thread stopped!")
                    break
            start_time = time.time()
            
            # Add type hint for the received data
            data: Dict[str, TelemetryData] = receiver._read_mmap()

            if data:
                ocon_data: TelemetryData = data['MyTeam1']
                ocon: Car = ocon_data.telemetry.car #To Export to CSV and Live-Telemetry
                
                gasly_data: TelemetryData = data['MyTeam2']
                gasly: Car = gasly_data.telemetry.car #To Export to CSV and Live-Telemetry

            # Adjust sleep to maintain 100Hz refresh rate
            elapsed = time.time() - start_time
            time.sleep(max(0, 0.01 - elapsed))
            
    except KeyboardInterrupt:
        print("\nExiting...")
    finally:
        receiver.close()