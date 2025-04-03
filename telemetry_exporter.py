import csv
import time
from datetime import datetime
from multiprocessing import Queue
from pathlib import Path
from telemetry_server import TelemetryReceiver

class TelemetryExporter:
    def __init__(self, export_queue=None):
        self.ocon_file = None
        self.gasly_file = None
        self.ocon_writer = None
        self.gasly_writer = None
        self.last_values = {
            "MyTeam1": {"turn": 0, "lap": 0},
            "MyTeam2": {"turn": 0, "lap": 0}
        }
        self._prepare_csv_files()

    def _prepare_csv_files(self):
        """Initialize CSV files with headers in the specified order."""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        Path("telemetry_data").mkdir(exist_ok=True)

        headers = [
            "timestamp", "trackName", "timeElapsed", "driverNumber",
            "pitstopStatus", "currentLap", "turnNumber", "compound",
            "speed", "rpm", "gear", "flTemp", "flDeg", "frTemp", "frDeg",
            "rlTemp", "rlDeg", "rrTemp", "rrDeg", "engineTemp", "engineDeg",
            "gearboxDeg", "ersDeg", "charge", "fuel", "paceMode", "fuelMode",
            "ersMode", "drsMode", "currentLapTime", "driverBestLap",
            "lastLapTime", "lastS1Time", "lastS2Time", "lastS3Time",
            "bestSessionTime", "rubber", "airTemp", "trackTemp", "weather"
        ]

        self.ocon_file = open(f"telemetry_data/ocon_{timestamp}.csv", "w", newline="")
        self.gasly_file = open(f"telemetry_data/gasly_{timestamp}.csv", "w", newline="")
        self.ocon_writer = csv.writer(self.ocon_file)
        self.gasly_writer = csv.writer(self.gasly_file)
        self.ocon_writer.writerow(headers)
        self.gasly_writer.writerow(headers)

    def _should_write(self, car_name: str, current_turn: int, current_lap: int) -> bool:
        """Determine if we should write based on turn/lap changes."""
        # Always write first data point
        if self.last_values[car_name]["turn"] == 0 and self.last_values[car_name]["lap"] == 0:
            return True
            
        # Write if turn or lap changed
        if (current_turn != self.last_values[car_name]["turn"] or 
            current_lap != self.last_values[car_name]["lap"]):
            return True
            
        return False

    def _process_row(self, car_data, is_lap_change: bool):
        """Prepare a row of data for CSV export in the specified order."""
        driver = car_data.telemetry.driver
        session = car_data.telemetry.session
        
        # Base data (always included)
        row = [
            datetime.now().isoformat(),  # timestamp
            session.trackName,           # trackName
            session.timeElasped,         # timeElapsed
            driver.driverNumber,         # driverNumber
            driver.pitstopStatus,        # pitstopStatus
            driver.status.currentLap,    # currentLap
            driver.status.turnNumber,     # turnNumber
            driver.car.tyres.compound,    # compound
            driver.car.speed,             # speed
            driver.car.rpm,               # rpm
            driver.car.gear,              # gear
            driver.car.tyres.temperature.flTemp,  # flTemp
            driver.car.tyres.wear.flDeg,          # flDeg
            driver.car.tyres.temperature.frTemp,  # frTemp
            driver.car.tyres.wear.frDeg,          # frDeg
            driver.car.tyres.temperature.rlTemp,  # rlTemp
            driver.car.tyres.wear.rlDeg,          # rlDeg
            driver.car.tyres.temperature.rrTemp,  # rrTemp
            driver.car.tyres.wear.rrDeg,          # rrDeg
            driver.car.components.engine.engineTemp,  # engineTemp
            driver.car.components.engine.engineDeg,   # engineDeg
            driver.car.components.gearbox.gearboxDeg, # gearboxDeg
            driver.car.components.ers.ersDeg,         # ersDeg
            driver.car.charge,                        # charge
            driver.car.fuel,                          # fuel
            driver.car.modes.paceMode,                # paceMode
            driver.car.modes.fuelMode,                # fuelMode
            driver.car.modes.ersMode,                 # ersMode
            driver.car.modes.drsMode,                 # drsMode
            driver.timings.currentLapTime             # currentLapTime
        ]

        # Only include these on lap changes
        if is_lap_change:
            row.extend([
                driver.timings.driverBestLap,         # driverBestLap
                driver.timings.lastLapTime,           # lastLapTime
                driver.timings.sectors.lastS1Time,    # lastS1Time
                driver.timings.sectors.lastS2Time,    # lastS2Time
                driver.timings.sectors.lastS3Time,   # lastS3Time
            ])
        else:
            row.extend([""] * 5)  # Empty placeholders

        row.extend([
            session.bestSessionTime,      # bestSessionTime
            session.rubber,               # rubber
            session.weather.airTemp,      # airTemp
            session.weather.trackTemp,    # trackTemp
            session.weather.weather       # weather
        ])
        
        return row

    def export_from_queue(self, queue: Queue):
        """Main export loop with turn/lap-based writing."""
        try:
            while True:
                data = queue.get()
                if not data:
                    continue

                for car_name in ["MyTeam1", "MyTeam2"]:
                    if car_name not in data:
                        continue

                    car_data = data[car_name]
                    current_turn = car_data.telemetry.driver.status.turnNumber
                    current_lap = car_data.telemetry.driver.status.currentLap
                    is_lap_change = (current_lap != self.last_values[car_name]["lap"])

                    if self._should_write(car_name, current_turn, current_lap):
                        row = self._process_row(car_data, is_lap_change)

                        if car_name == "MyTeam1":
                            self.ocon_writer.writerow(row)
                            self.ocon_file.flush()
                        else:
                            self.gasly_writer.writerow(row)
                            self.gasly_file.flush()

                        # Update last recorded values
                        self.last_values[car_name]["turn"] = current_turn
                        self.last_values[car_name]["lap"] = current_lap

        except KeyboardInterrupt:
            print("\nExiting exporter gracefully...")
        finally:
            if self.ocon_file:
                self.ocon_file.close()
            if self.gasly_file:
                self.gasly_file.close()

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