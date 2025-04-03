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
# DATA STRUCTURE DEFINITIONS (Updated to match Lua structure)
# =============================================
#region
@dataclass
class TyreTemperature:
    flTemp: float
    frTemp: float
    rlTemp: float
    rrTemp: float

@dataclass
class TyreWear:
    flDeg: float
    frDeg: float
    rlDeg: float
    rrDeg: float

@dataclass
class Tyres:
    compound: str
    temperature: TyreTemperature
    wear: TyreWear

@dataclass
class Engine:
    engineTemp: float
    engineDeg: float

@dataclass
class Gearbox:
    gearboxDeg: float

@dataclass
class ERS:
    ersDeg: float

@dataclass
class Components:
    engine: Engine
    gearbox: Gearbox
    ers: ERS

@dataclass
class Modes:
    paceMode: str
    fuelMode: str
    ersMode: str
    drsMode: str

@dataclass
class SectorTimes:
    lastS1Time: float
    lastS2Time: float
    lastS3Time: float

@dataclass
class Timings:
    currentLapTime: float
    driverBestLap: float
    lastLapTime: float
    sectors: SectorTimes

@dataclass
class Status:
    turnNumber: int
    currentLap: int

@dataclass
class CarTelemetry:
    speed: float
    rpm: float
    gear: int
    charge: float
    fuel: float
    tyres: Tyres
    modes: Modes
    components: Components

@dataclass
class Weather:
    airTemp: float
    trackTemp: float
    weather: str

@dataclass
class Session:
    timeElasped: float
    trackName: str
    bestSessionTime: float
    rubber: float
    weather: Weather

@dataclass
class Driver:
    driverNumber: int
    pitstopStatus: str
    timings: Timings
    status: Status
    car: CarTelemetry

@dataclass
class Telemetry:
    session: Session
    driver: Driver
#endregion

class TelemetryData:
    def __init__(self, data_dict):
        # Handle the nested structure properly
        if 'telemetry' in data_dict:
            data_dict = data_dict['telemetry']
        
        # Convert nested dictionaries to dataclasses
        self.telemetry = self._convert_to_dataclass(data_dict, Telemetry)
    
    def _convert_to_dataclass(self, data, dataclass_type):
        if not isinstance(data, dict):
            return data

        fields = dataclass_type.__dataclass_fields__

        kwargs = {}
        for field_name, field_info in fields.items():
            field_value = data.get(field_name)

            if field_value is None:
                kwargs[field_name] = field_info.default if hasattr(field_info, 'default') else None
                continue
                
            field_type = field_info.type

            # Handle nested dataclasses
            if hasattr(field_type, '__dataclass_fields__'):
                # Special case for weather which comes as dict
                if field_name == 'weather' and isinstance(field_value, dict):
                    kwargs[field_name] = field_type(**field_value)
                else:
                    kwargs[field_name] = self._convert_to_dataclass(field_value, field_type)
            else:
                kwargs[field_name] = field_value

        return dataclass_type(**kwargs)

# =============================================
# RECEIVER CLASS (unchanged)
# =============================================
class TelemetryReceiver:
    def __init__(self, export_queue: Queue = None, plot_queue: Queue = None):
        self.mmf = None
        self.file = None
        self.export_queue = export_queue
        self.plot_queue = plot_queue
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
        while self.running:
            try:
                data = self._read_mmap()
                if data:
                    if self.export_queue:
                        try:
                            self.export_queue.put_nowait(data)
                        except self.export_queue.full():
                            self.export_queue.empty
                            print("Export queue full - dropping data")

                    if self.plot_queue and not self.plot_queue.full():
                        try:
                            self.plot_queue.put_nowait(data)
                        except self.plot_queue.full():
                            self.plot_queue.empty
                            print("Plot queue full - dropping data")
            except Exception as e:
                print(f"Broadcast error: {e}")
                time.sleep(0.1)

    def _read_mmap(self):
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
# MAIN PROGRAM (unchanged)
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
            
            data: Dict[str, TelemetryData] = receiver._read_mmap()

            if data:
                ocon_data: TelemetryData = data['MyTeam1']
                ocon: Driver = ocon_data.telemetry.driver
                print(ocon_data.telemetry)
                
                gasly_data: TelemetryData = data['MyTeam2']
                gasly: Driver = gasly_data.telemetry.driver

            elapsed = time.time() - start_time
            time.sleep(max(0, 0.01 - elapsed))
            
    except KeyboardInterrupt:
        print("\nExiting...")
    finally:
        receiver.close()