import csv
import time
import configparser
from datetime import datetime
from multiprocessing import Queue
from queue import Empty
from pathlib import Path
from telemetry_server import TelemetryReceiver

config = configparser.ConfigParser()
config.read('settings.ini')

class TelemetryExporter:
    def __init__(self, export_queue=None):
        self.car_files = {}
        self.car_writers = {}
        self.last_values = {}
        self.tracked_cars = self._get_tracked_cars()
        self._prepare_csv_files()
    
    def _get_tracked_cars(self):
        """Get list of cars to track from settings.ini"""
        cars = config.get("CSV Settings", "TRACKED_DRIVERS")
        return [c.strip() for c in cars.split(',')]

    def stop(self):
        """Close all CSV files."""
        for file in self.car_files.values():
            if file:
                file.close()
        print("Exporter shutdown complete")

    def _prepare_csv_files(self):
        """Initialize CSV files with headers for each tracked car."""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        try:
            Path(config.get("CSV", "CSV_PATH")).mkdir(exist_ok=True)
        except Exception as e:
            print(f"Error creating directory: {e}")
            return

        headers = [
            "timestamp", "trackName", "timeElapsed", "driverNumber",
            "pitstopStatus", "currentLap", "turnNumber", "position", "compound",
            "speed", "rpm", "gear", "flTemp", "flDeg", "frTemp", "frDeg",
            "rlTemp", "rlDeg", "rrTemp", "rrDeg", "engineTemp", "engineDeg",
            "gearboxDeg", "ersDeg", "charge", "fuel", "paceMode", "fuelMode",
            "ersMode", "drsMode", "currentLapTime", "driverBestLap",
            "lastLapTime", "lastS1Time", "lastS2Time", "lastS3Time",
            "bestSessionTime", "rubber", "airTemp", "trackTemp", "weather"
        ]

        for car in self.tracked_cars:
            filename = f"{config.get('CSV', 'CSV_PATH')}{car}_{timestamp}.csv"
            
            try:
                file = open(filename, "w", newline="")
                writer = csv.writer(file)
                writer.writerow(headers)
                
                self.car_files[car] = file
                self.car_writers[car] = writer
                self.last_values[car] = {"turn": 0, "lap": 0}
                
            except Exception as e:
                print(f"Error creating file for {car}: {e}")

    def _should_write(self, car_name: str, current_turn: int, current_lap: int, pitstopStatus: str) -> bool:
        """Determine if we should write based on turn/lap changes."""
        if car_name not in self.last_values:
            return False
            
        # Check if car is in the garage
        if pitstopStatus == "In Garage":
            return False
        
        # Always write first data point
        if self.last_values[car_name]["turn"] == 0 and self.last_values[car_name]["lap"] == 0:
            return True
            
        # Write if turn or lap changed
        if (current_turn != self.last_values[car_name]["turn"] or 
            current_lap != self.last_values[car_name]["lap"]):
            return True
            
        return False

    def _process_row(self, car_data):
        try:
            t = car_data.telemetry
            return [
                datetime.now().isoformat(),
                t.session.trackName,
                t.session.timeElapsed,
                t.driver.driverNumber,
                t.driver.pitstopStatus,
                t.driver.status.currentLap,
                t.driver.status.turnNumber,
                t.driver.position + 1,  # Adjust for zero-based index
                t.driver.car.tyres.compound,
                t.driver.car.speed,
                t.driver.car.rpm,
                t.driver.car.gear,
                t.driver.car.tyres.temperature.flTemp,
                t.driver.car.tyres.wear.flDeg,
                t.driver.car.tyres.temperature.frTemp,
                t.driver.car.tyres.wear.frDeg,
                t.driver.car.tyres.temperature.rlTemp,
                t.driver.car.tyres.wear.rlDeg,
                t.driver.car.tyres.temperature.rrTemp,
                t.driver.car.tyres.wear.rrDeg,
                t.driver.car.components.engine.engineTemp,
                t.driver.car.components.engine.engineDeg,
                t.driver.car.components.gearbox.gearboxDeg,
                t.driver.car.components.ers.ersDeg,
                t.driver.car.charge,
                t.driver.car.fuel,
                t.driver.car.modes.paceMode,
                t.driver.car.modes.fuelMode,
                t.driver.car.modes.ersMode,
                t.driver.car.modes.drsMode,
                t.driver.timings.currentLapTime,
                t.driver.timings.driverBestLap,
                t.driver.timings.lastLapTime,
                t.driver.timings.sectors.lastS1Time,
                t.driver.timings.sectors.lastS2Time,
                t.driver.timings.sectors.lastS3Time,
                t.session.bestSessionTime,
                t.session.rubber,
                t.session.weather.airTemp,
                t.session.weather.trackTemp,
                t.session.weather.weather
            ]
        except Exception as e:
            print(f"Error processing row: {e}")
            return None

    def export_from_queue(self, queue: Queue):
        try:
            while True:
                try:
                    data = queue.get(timeout=1)
                    if not data:
                        continue

                    for car_name in self.tracked_cars:
                        if car_name not in data:
                            continue

                        car_data = data[car_name]
                        if not hasattr(car_data, 'telemetry') or not hasattr(car_data.telemetry, 'driver'):
                            print(f"Invalid data structure for {car_name}")
                            continue

                        current_turn = car_data.telemetry.driver.status.turnNumber
                        current_lap = car_data.telemetry.driver.status.currentLap
                        pitstopStatus = car_data.telemetry.driver.pitstopStatus

                        if self._should_write(car_name, current_turn, current_lap, pitstopStatus):
                            row = self._process_row(car_data)

                            if row and car_name in self.car_writers:
                                self.car_writers[car_name].writerow(row)
                                self.car_files[car_name].flush()
                                self.last_values[car_name]["turn"] = current_turn
                                self.last_values[car_name]["lap"] = current_lap

                except Empty:
                    continue
                except Exception as e:
                    print(f"Error processing data: {e}")
                    continue

        except KeyboardInterrupt:
            print("\nExiting exporter gracefully...")
        finally:
            self.stop()

if __name__ == "__main__":
    print("F1 Telemetry Exporter - Running standalone")
    export_queue = Queue()
    receiver = TelemetryReceiver(export_queue=export_queue)
    
    if not receiver.connect():
        exit(1)

    exporter = TelemetryExporter()
    try:
        exporter.export_from_queue(export_queue)
    finally:
        receiver.close()