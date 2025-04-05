import mmap
import struct
import json
import time
from pymem import Pymem
from pymem.process import module_from_name

# Configuration
SHARED_MEM_NAME = "F1Manager_Telemetry"
SHARED_MEM_SIZE = 65536
FULL_POINTER_PATH = ["F1Manager24.exe", 0x798F570, 0x150, 0x3E8, 0x130, 0x0, 0x28, 0x0]

# Enum mapping
tyre_compound_map = {
    0: "Soft", 1: "Soft", 2: "Soft", 3: "Soft", 4: "Soft", 5: "Soft", 6: "Soft", 7: "Soft",
    8: "Medium", 9: "Medium", 10: "Medium",
    11: "Hard", 12: "Hard",
    13: "Inter", 14: "Inter", 15: "Inter", 16: "Inter", 17: "Inter",
    18: "Wet", 19: "Wet"
}

pitstop_status_map = {
    0: "None", 1: "Requested", 2: "Entering", 3: "Queuing", 4: "Stopped", 
    5: "Exiting", 6: "In Garage", 7: "Jack Up", 8: "Releasing", 
    9: "Car Setup", 10: "Pit Stop Approach", 11: "Pit Stop Penalty", 12: "Waiting for Release"
}

pace_mode_map = {
    4: "Conserve", 3: "Light", 2: "Standard", 1: "Aggressive", 0: "Attack"
}

fuel_mode_map = {
    2: "Conserve", 1: "Balanced", 0: "Push"
}

ers_mode_map = {
    0: "Neutral", 1: "Harvest", 2: "Deploy", 3: "Top Up"
}

drs_map = {
    0: "Disabled", 1: "Detected", 2: "Enabled", 3: "Active"
}

weather_map = {
    0: "None", 1: "Sunny", 2: "Partly Sunny", 4: "Cloudy", 
    8: "Light Rain", 16: "Moderate Rain", 32: "Heavy Rain"
}

track_name_map = {
    0: "INVALID",
    1: "Albert Park",
    2: "Bahrain",
    3: "Shanghai",
    4: "Baku",
    5: "Barcelona",
    6: "Monaco",
    7: "Montreal",
    8: "PaulRicard",
    9: "RedBull Ring",
    10: "Silverstone",
    11: "Jeddah",
    12: "Hungaroring",
    13: "Spa-Francorchamps",
    14: "Monza",
    15: "Marina Bay",
    16: "Sochi",
    17: "Suzuka",
    18: "Hermanos Rodriguez",
    19: "Circuit Of The Americas",
    20: "Interlagos",
    21: "Yas Marina",
    22: "Miami",
    23: "Zandvoort",
    24: "Imola",
    25: "Vegas",
    26: "Qatar"
}

cars_offsets = {
    "Ferrari1": 0x0000,
    "Ferrari2": 0x10D8,
    "McLaren1": 0x21B0,
    "McLaren2": 0x3288,
    "RedBull1": 0x4360,
    "RedBull2": 0x5438,
    "Mercedes1": 0x6510,
    "Mercedes2": 0x75E8,
    "Alpine1": 0x86C0,
    "Alpine2": 0x9798,
    "Williams1": 0xA870,
    "Williams2": 0xB948,
    "Haas1": 0xCA20,
    "Haas2": 0xDAF8,
    "RacingBulls1": 0xEBD0,
    "RacingBulls2": 0xFCA8,
    "KickSauber1": 0x10D80,
    "KickSauber2": 0x11E58,
    "AstonMartin1": 0x12F30,
    "AstonMartin2": 0x14008,
    "MyTeam1": 0x150E0,
    "MyTeam2": 0x161B8
}

data_structure = {
    "session": {
        "timeElasped": {"source": "session", "offset": 0x148, "type": "float", "default": 0},
        "rubber": {"source": "session", "offset": 0x278, "type": "float", "default": 0},
        "bestSessionTime": {"source": "session", "offset": 0x768, "type": "float", "default": 0},
        "trackID": {"source": "session", "offset": 0x228, "type": "byte", "enum": "trackName", "default": "Unknown"}
    },
    "driver": {
        "driverNumber": {"source": "driver", "offset": 0x58C, "type": "byte", "default": 0},
        "turnNumber": {"source": "driver", "offset": 0x530, "type": "integer", "default": 0},
        "speed": {"source": "driver", "offset": 0x4F0, "type": "integer", "default": 0},
        "rpm": {"source": "driver", "offset": 0x4EC, "type": "integer", "default": 0},
        "gear": {"source": "driver", "offset": 0x524, "type": "integer", "default": 0},
        "drsMode": {"source": "driver", "offset": 0x521, "type": "byte", "enum": "drs", "default": "Unknown"},
        "driverBestLap": {"source": "driver", "offset": 0x538, "type": "float", "default": 0},
        "lastLapTime": {"source": "driver", "offset": 0x540, "type": "float", "default": 0},
        "currentLapTime": {"source": "driver", "offset": 0x544, "type": "float", "default": 0},
        "lastS1Time": {"source": "driver", "offset": 0x548, "type": "float", "default": None},
        "lastS2Time": {"source": "driver", "offset": 0x550, "type": "float", "default": None},
        "lastS3Time": {"source": "driver", "offset": 0x558, "type": "float", "default": None}
    },
    "car": {
        "currentLap": {"source": "car", "offset": 0x7E4, "type": "integer", "default": 0},
        "tyreCompound": {"source": "car", "offset": 0xED5, "type": "byte", "enum": "tyreCompound", "default": "Unknown"},
        "pitstopStatus": {"source": "car", "offset": 0x8A8, "type": "byte", "enum": "pitstopStatus", "default": "Unknown"},
        "paceMode": {"source": "car", "offset": 0xEF1, "type": "byte", "enum": "paceMode", "default": "Unknown"},
        "fuelMode": {"source": "car", "offset": 0xEF0, "type": "byte", "enum": "fuelMode", "default": "Unknown"},
        "ersMode": {"source": "car", "offset": 0xEF2, "type": "byte", "enum": "ersMode", "default": "Unknown"},
        "flTemp": {"source": "car", "offset": 0x980, "type": "float", "default": 0},
        "flDeg": {"source": "car", "offset": 0x984, "type": "float", "default": 0},
        "frTemp": {"source": "car", "offset": 0x98C, "type": "float", "default": 0},
        "frDeg": {"source": "car", "offset": 0x990, "type": "float", "default": 0},
        "rlTemp": {"source": "car", "offset": 0x998, "type": "float", "default": 0},
        "rlDeg": {"source": "car", "offset": 0x99C, "type": "float", "default": 0},
        "rrTemp": {"source": "car", "offset": 0x9A4, "type": "float", "default": 0},
        "rrDeg": {"source": "car", "offset": 0x9A8, "type": "float", "default": 0},
        "engineTemp": {"source": "car", "offset": 0x77C, "type": "float", "default": 0},
        "engDeg": {"source": "car", "offset": 0x784, "type": "float", "default": 0},
        "gearboxDeg": {"source": "car", "offset": 0x78C, "type": "float", "default": 0},
        "ersDeg": {"source": "car", "offset": 0x788, "type": "float", "default": 0},
        "charge": {"source": "car", "offset": 0x878, "type": "float", "default": 0},
        "fuel": {"source": "car", "offset": 0x778, "type": "float", "default": 0}
    },
    "weather": {
        "airTemp": {"source": "weather", "offset": 0xAC, "type": "float", "default": 0},
        "trackTemp": {"source": "weather", "offset": 0xB0, "type": "float", "default": 0},
        "weather": {"source": "weather", "offset": 0xBC, "type": "byte", "enum": "weather", "default": "Unknown"}
    },
    "pointers": {
        "pilotDataptr": {"source": "car", "offset": 0x708, "type": "pointer"},
        "sessionDataptr": {"source": "driver", "offset": 0x940, "type": "pointer"},
        "weatherDataptr": {"source": "session", "offset": 0xA12990, "type": "pointer"}
    }
}

enum_maps = {
    "pitstopStatus": pitstop_status_map,
    "paceMode": pace_mode_map,
    "fuelMode": fuel_mode_map,
    "ersMode": ers_mode_map,
    "drs": drs_map,
    "weather": weather_map,
    "tyreCompound": tyre_compound_map,
    "trackName": track_name_map,
}

class F1ManagerDataExporter:
    def __init__(self):
        self.pm = None
        self.base_address = None
        self.mmf = None
        self.proccess_handle = None
        
    def initialize(self):
        try:
            self.pm = Pymem("F1Manager24.exe")
            self.proccess_handle = self.pm.process_handle
            module = module_from_name(self.proccess_handle, FULL_POINTER_PATH[0])
            
            if not module:
                print("Failed to find F1Manager24.exe module.")
                return False
            
            self.base_address = module.lpBaseOfDll
            for offset in FULL_POINTER_PATH[1:]:
                self.base_address = self.pm.read_int(self.base_address + offset)
                
                if self.base_address == 0:
                    print("Failed to follow pointer path.")
                    return False
            
            try:
                self.mmf = mmap.mmap(-1, SHARED_MEM_SIZE, SHARED_MEM_NAME)
                self.mmf.write(b"\0" * SHARED_MEM_SIZE)
                self.mmf.seek(0)
                return True
            except Exception as e:
                print(f"Failed to create shared memory: {e}")
                return False
        except Exception as e:
            print(f"Initialization error: {e}")
            return False
    
    def read_memory(self, address, data_type, enum = None):
        if address == 0 or address is None:
            return None
        
        try:
            if data_type == "byte":
                value = self.pm.read_uchar(address)
            elif data_type == "integer":
                value = self.pm.read_int(address)
            elif data_type == "float":
                value = self.pm.read_float(address)
            elif data_type == "pointer":
                value = self.pm.read_int(address)
            else:
                return None
            
            if enum and enum in enum_maps:
                enum_map = enum_maps[enum]
                return enum_map.get(value, "Unknown")
            
            return value
        except:
            return None
    
    def collect_driver_data(self, car_name):
        if not self.base_address:
            time.sleep(0.5)
            return None
        
        car_offset = cars_offsets.get(car_name)
        if car_offset is None:
            return None
        
        car_base = self.base_address + car_offset
        if car_base == 0:
            return None
        
        bases = {
            "car": car_base,
            "driver": self.read_memory(car_base + data_structure["pointers"]["pilotDataptr"]["offset"], "pointer"),
            "session": None,
            "weather": None
        }
        
        if bases["driver"]:
            bases["session"] = self.read_memory(
                bases["driver"] + data_structure["pointers"]["sessionDataptr"]["offset"], "pointer"
            )
            
            if bases["session"]:
                bases["weather"] = self.read_memory(
                    bases["session"] + data_structure["pointers"]["weatherDataptr"]["offset"], "pointer"
                )
        
        result = {}
        
        for category, fields in data_structure.items():
            if category != "pointers":
                result[category] = {}
                for field_name, field_def, in fields.items():
                    if field_def["source"] and bases[field_def["source"]]:
                        address = bases[field_def["sources"]] + field_def["offset"]
                        value = self.read_memory(address, field_def["type"], field_def.get("enum"))
                        result[category][field_name] = value if value is not None else field_def["default"]
        return result

    def send_data(self):
        all_data = {}
        
        for car_name in cars_offsets.keys():
            raw_data = self.collect_driver_data(car_name)
            if raw_data:
                all_data[car_name] = {
                    "telemetry": {
                        "session": {
                            "timeElasped": raw_data["session"].get("timeElasped", 0),
                            "trackName": raw_data["session"].get("trackID", "Unknown"),
                            "bestSessionTime": raw_data["session"].get("bestSessionTime", 0),
                            "rubber": raw_data["session"].get("rubber", 0),
                            "weather": {
                                "airTemp": raw_data["weather"].get("airTemp", 0),
                                "trackTemp": raw_data["weather"].get("trackTemp", 0),
                                "weather": raw_data["weather"].get("weather", "Unknown")
                            }
                        },
                        "driver": {
                            "driverNumber": raw_data["driver"].get("driverNumber", 0),
                            "pitstopStatus": raw_data["car"].get("pitstopStatus", "Unknown"),
                            "status": {
                                "turnNumber": raw_data["driver"].get("turnNumber", 0),
                                "currentLap": raw_data["car"].get("currentLap", 0)
                            },
                            "timings": {
                                "currentLapTime": raw_data["driver"].get("currentLapTime", 0),
                                "driverBestLap": raw_data["driver"].get("driverBestLap", 0),
                                "lastLapTime": raw_data["driver"].get("lastLapTime", 0),
                                "sectors": {
                                    "lastS1Time": raw_data["driver"].get("lastS1Time", 0),
                                    "lastS2Time": raw_data["driver"].get("lastS2Time", 0),
                                    "lastS3Time": raw_data["driver"].get("lastS3Time", 0)
                                }
                            },
                            "car": {
                                "speed": raw_data["driver"].get("speed", 0),
                                "rpm": raw_data["driver"].get("rpm", 0),
                                "gear": raw_data["driver"].get("gear", 0),
                                "charge": raw_data["car"].get("charge", 0),
                                "fuel": raw_data["car"].get("fuel", 0),
                                "tyres": {
                                    "compound": raw_data["car"].get("tyreCompound", "Unknown"),
                                    "temperature": {
                                        "flTemp": raw_data["car"].get("flTemp", 0),
                                        "frTemp": raw_data["car"].get("frTemp", 0),
                                        "rlTemp": raw_data["car"].get("rlTemp", 0),
                                        "rrTemp": raw_data["car"].get("rrTemp", 0)
                                    },
                                    "wear": {
                                        "flDeg": raw_data["car"].get("flDeg", 0),
                                        "frDeg": raw_data["car"].get("frDeg", 0),
                                        "rlDeg": raw_data["car"].get("rlDeg", 0),
                                        "rrDeg": raw_data["car"].get("rrDeg", 0)
                                    }
                                },
                                "modes": {
                                    "paceMode": raw_data["car"].get("paceMode", "Unknown"),
                                    "fuelMode": raw_data["car"].get("fuelMode", "Unknown"),
                                    "ersMode": raw_data["car"].get("ersMode", "Unknown"),
                                    "drsMode": raw_data["driver"].get("drsMode", "Unknown")
                                },
                                "components": {
                                    "engine": {
                                        "engineTemp": raw_data["car"].get("engineTemp", 0),
                                        "engineDeg": raw_data["car"].get("engDeg", 0)
                                    },
                                    "gearbox": {
                                        "gearboxDeg": raw_data["car"].get("gearboxDeg", 0)
                                    },
                                    "ers": {
                                        "ersDeg": raw_data["car"].get("ersDeg", 0)
                                    }
                                }
                            }
                        }
                    }
                }
            
        self.write_structured_data(all_data)
    
    def write_structured_data(self, data):
        if not self.mmf:
            return False
        
        try:
            json_str = json.dumps(data)
            json_bytes = json_str.encode('utf-8')
            
            self.mmf.seek(0)
            self.mmf.write(struct.pack('<I', len(json_bytes)))
            self.mmf.write(json_bytes)
            self.mmf.flush()
            return True
        except Exception as e:
            print(f"Failed to write data to shared memory: {e}")
            return False
    
    def run(self):
        if not self.initialize():
            return
        
        try:
            while True:
                self.send_data()
                time.sleep(0.01)
        except KeyboardInterrupt:
            print("Exiting...")
        
        finally:
            if self.mmf:
                self.mmf.close()
            if self.pm:
                self.pm.close_process()

if __name__ == "__main__":
    exporter = F1ManagerDataExporter()
    exporter.run()