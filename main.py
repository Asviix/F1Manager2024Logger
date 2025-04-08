import os
import shutil
import sys
import time
import psutil
import multiprocessing
import configparser
import subprocess
import win32con
import win32gui
import win32process
from multiprocessing import Process, Manager
from pathlib import Path
from telemetry_server import TelemetryReceiver
from telemetry_exporter import TelemetryExporter
from telemetry_plotter import TelemetryPlotter

config = configparser.ConfigParser()
config.read('settings.ini')

def kill_cheat_engine():
    try:
        for proc in psutil.process_iter(['name']):
            if proc.info['name'] and 'cheatengine' in proc.info['name'].lower():
                try:
                    proc.terminate()
                    proc.wait(timeout=2)
                except (psutil.NoSuchProcess, psutil.TimeoutExpired):
                    continue
        print("[Main] Successfully terminated Cheat Engine processes")
    except Exception as e:
        print(f"[Main] Error terminating Cheat Engine: {str(e)}")

def cleanup_shared_memory():
    mem_file = Path("F1Manager_Telemetry")
    max_attempts = 3
    delay = 1
    
    for attempt in range(max_attempts):
        try:
            if mem_file.exists():
                mem_file.unlink()
                print("[Main] Successfully deleted shared memory file")
                return
        except Exception as e:
            if attempt == max_attempts - 1:
                print(f"[Main] Failed to delete shared memory file after {max_attempts} attempts: {str(e)}")
            time.sleep(delay)

def launch_cheat_engine_table():
    try:
        ct_path = Path(config.get("CheatEngine", "CT_PATH"))
        ce_path = Path(config.get("CheatEngine", "CE_PATH"))
        
        if not ct_path.exists() or not ce_path.exists():
            raise FileNotFoundError("Required files not found")
        
        proc = subprocess.Popen([str(ce_path), str(ct_path)])
        print("[Main] Launched Cheat Engine, hiding window...")
        
        def hide_ce_window():
            hwnd = win32gui.FindWindow(None, "Cheat Engine")
            if hwnd:
                win32gui.ShowWindow(hwnd, win32con.SW_HIDE)
                return True
            return False
        
        time.sleep(1)
        if hide_ce_window():
            print("[Main] Cheat Engine window hidden successfully")
            return True
        
        print("[Main] Warning: Could not hide Cheat Engine window")
        return True
        
    except Exception as e:
        print(f"[Main] Error launching Cheat Engine: {str(e)}")
        return False

def run_telemetry_server(export_queue, plot_queue, enabled):
    receiver = None
    try:
        print("[Server] Starting with shared queue")
        receiver = TelemetryReceiver(export_queue=export_queue, plot_queue=plot_queue)
        if not receiver.connect():
            sys.exit(1)
            
        while receiver.running:
            time.sleep(0.01)
            
    except Exception as e:
        print(f"SERVER CRASHED: {e}")
        raise
    finally:
        if receiver:
            print("\n[Server] Exiting server gracefully...")
            receiver.close()

def run_telemetry_exporter(export_queue):
    exporter = None
    try:
        print("[Exporter] Starting with shared queue")
        exporter = TelemetryExporter(export_queue=export_queue)
        
        while True:
            try:
                if not export_queue.empty():
                    exporter.export_from_queue(export_queue)
                time.sleep(0.01)
            except Exception as e:
                print(f"EXPORTER ERROR: {e}")
                time.sleep(1)  # Wait before retrying

    finally:
        if exporter:
            print("\n[Exporter] Exiting exporter gracefully...")
            exporter.stop()

def run_telemetry_plotter(plot_queue):
    plotter = None
    try:
        print("[Plotter] Starting with shared queue")
        plotter = TelemetryPlotter(plot_queue=plot_queue)
        
        while True:
            try:
                if not plot_queue.empty():
                    plotter.run(plot_queue)
                time.sleep(1)
            except Exception as e:
                print(f"PLOTTER ERROR: {e}")
                time.sleep(1)
    except KeyboardInterrupt:
        pass
    finally:
        if plotter:
            print("\n[Plotter] Exiting plotter gracefully...")
            plotter.stop()

def main():
    # Set multiprocessing start method for Windows
    if os.name == 'nt':
        try:
            multiprocessing.set_start_method('spawn')
        except RuntimeError:
            pass  # Already set, ignore

    print("\nF1 Telemetry System - Initializing...")
    print("----------------------------------")
    
    if not launch_cheat_engine_table():
        sys.exit(1)
    
    # Use Manager for cross-process queues
    with Manager() as manager:
        plot_queue = manager.Queue(maxsize=500) if config.getboolean("Plotter Settings", "ENABLE_PLOTTER") else None
        export_queue = manager.Queue(maxsize=100) if config.getboolean("CSV", "LOG_TO_CSV") else None
        
        process_definitions = [
            {
                'target': run_telemetry_server,
                'args': (export_queue, plot_queue, True),  # Server always enabled
                'enabled': True  # Server is mandatory
            },
            {
                'target': run_telemetry_exporter,
                'args': (export_queue,),
                'enabled': config.getboolean("CSV", "LOG_TO_CSV")
            },
            {
                'target': run_telemetry_plotter,
                'args': (plot_queue,),
                'enabled': config.getboolean("Plotter Settings", "ENABLE_PLOTTER")
            }
        ]
        
        try:
            print("-------------------------------------------")
            print("[Main] Starting telemetry components...")
            processes = []
            for definition in process_definitions:
                if definition['enabled']:
                    p = Process(target=definition['target'], args=definition['args'])
                    p.start()
                    processes.append(p)
                    time.sleep(1)  # Stagger process start times
            
            print("-------------------------------------------")
            print("System operational - Press Ctrl+C to shutdown")
            print("-------------------------------------------")
            
            # Enhanced process monitoring
            while True:
                dead = [p for p in processes if not p.is_alive()]
                if dead:
                    print(f"CRASHED: {[p.pid for p in dead]}")
                    break
                
        except KeyboardInterrupt:
            print("\n[Main] Shutdown signal received...")
        finally:
            print("\n[Main] Terminating processes...\n")
            for p in processes:
                if p.is_alive():
                    p.terminate()
                p.join()
            
            print("\n[Main] Closing Cheat Engine...")
            kill_cheat_engine()
            
            print("\n[Main] Cleaning up shared memory...")
            cleanup_shared_memory()
            
            print("\n[Main] Cleaning up pycache files...")
            try:
                shutil.rmtree("__pycache__", ignore_errors=True)
                print("[Main] Successfully deleted pycache")
            except Exception as e:
                print(f"[Main] Error cleaning up pycache: {str(e)}")
            
            print("\n-------------------------------------------")
            print("[Main] System shutdown complete")

if __name__ == "__main__":
    main()