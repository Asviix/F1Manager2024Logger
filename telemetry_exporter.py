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
        """Initialize CSV files with headers."""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        Path("telemetry_data").mkdir(exist_ok=True)

        headers = [
            "timestamp", "time_elapsed", "driver_number",
            "current_lap", "turn_number", "speed", "gear", "rpm",
            "current_lap_time", "tyre_compound",
            "fl_temp", "fl_deg", "fr_temp", "fr_deg",
            "rl_temp", "rl_deg", "rr_temp", "rr_deg",
            "pace_mode", "fuel_mode", "ers_mode",
            "engine_temp", "engine_deg", "gearbox_deg", "ers_deg",
            "charge", "fuel", "best_session_time",
            "driver_best_lap", "last_lap_time",
            "last_s1_time", "last_s2_time", "last_s3_time",
            "rubber", "air_temp", "track_temp"
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
        """Prepare a row of data for CSV export."""
        car = car_data.telemetry.car
        session = car_data.telemetry.session
        
        # Base data (always included)
        row = [
            datetime.now().isoformat(),
            session.time_elapsed,
            car.driverNumber,
            car.lap.current,
            car.lap.position,
            car.speed,
            car.gear,
            car.rpm,
            car.lap.time.current,
            car.tyres.compound,
            car.tyres.temps.front_left,
            car.tyres.wear.front_left,
            car.tyres.temps.front_right,
            car.tyres.wear.front_right,
            car.tyres.temps.rear_left,
            car.tyres.wear.rear_left,
            car.tyres.temps.rear_right,
            car.tyres.wear.rear_right,
            car.modes.pace,
            car.modes.fuel,
            car.modes.ers,
            car.components.engine.temp,
            car.components.engine.deg,
            car.components.gearbox.deg,
            car.components.ers.deg,
            car.charge,
            car.fuel,
        ]

        # Only include these on lap changes
        if is_lap_change:
            row.extend([
                session.best_time,
                car.lap.time.best,
                car.lap.time.last,
                car.lap.time.sector.last.s1,
                car.lap.time.sector.last.s2,
                car.lap.time.sector.last.s3,
            ])
        else:
            row.extend([""] * 6)  # Empty placeholders

        row.extend([
            session.track.rubber,
            session.weather.air_temp,
            session.track.temp
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
                    current_turn = car_data.telemetry.car.lap.position
                    current_lap = car_data.telemetry.car.lap.current
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