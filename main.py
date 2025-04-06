import os
import sys
import time
import psutil
import multiprocessing
from multiprocessing import Process, Manager
from pathlib import Path
from telemetry_server import TelemetryReceiver
from telemetry_exporter import TelemetryExporter
#from telemetry_plotter import TelemetryPlotter

def kill_cheat_engine():
    """Terminate all Cheat Engine processes"""
    try:
        for proc in psutil.process_iter(['name']):
            if proc.info['name'] and 'cheatengine' in proc.info['name'].lower():
                try:
                    proc.terminate()
                    proc.wait(timeout=2)
                except (psutil.NoSuchProcess, psutil.TimeoutExpired):
                    continue
        print("Successfully terminated Cheat Engine processes")
    except Exception as e:
        print(f"Error terminating Cheat Engine: {str(e)}")

def cleanup_shared_memory():
    """Delete the shared memory file with retries"""
    mem_file = Path("F1Manager_Telemetry")
    max_attempts = 3
    delay = 1
    
    for attempt in range(max_attempts):
        try:
            if mem_file.exists():
                mem_file.unlink()
                print("Successfully deleted shared memory file")
                return
        except Exception as e:
            if attempt == max_attempts - 1:
                print(f"Failed to delete shared memory file after {max_attempts} attempts: {str(e)}")
            time.sleep(delay)

def launch_cheat_engine_table():
    """Launch the Cheat Engine table file"""
    try:
        ct_path = Path("LoggingTable.ct")
        if not ct_path.exists():
            raise FileNotFoundError(f"Cheat Engine table not found at {ct_path.resolve()}")
        
        os.startfile(str(ct_path))
        print("Launched Cheat Engine table")
        time.sleep(5)  # Give CE time to attach to game
        return True
    except Exception as e:
        print(f"Error launching Cheat Engine: {str(e)}")
        return False

def run_telemetry_server(export_queue, plot_queue):
    """Run the telemetry server component"""
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
            receiver.close()

def run_telemetry_exporter(export_queue):
    """Run the CSV exporter component"""
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
                
    except KeyboardInterrupt:
        pass
    finally:
        if exporter:
            exporter.close()

'''def run_telemetry_plotter(plot_queue):
    plotter = None
    try:
        print("[Plotter] Starting with shared queue")
        plotter = TelemetryPlotter(plot_queue=plot_queue)
        
        plotter.run()
    
    except Exception as e:
        print(f"PLOTTER ERROR: {e}")
        time.sleep(1)
    except KeyboardInterrupt:
        pass
    finally:
        if plotter:
            plotter.stop()'''

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
        plot_queue = manager.Queue(maxsize=500)
        export_queue = manager.Queue(maxsize=100)
        
        processes = [
            Process(target=run_telemetry_server, args=(export_queue, plot_queue)),
            Process(target=run_telemetry_exporter, args=(export_queue,)),
            #Process(target=run_telemetry_plotter, args=(plot_queue,))
        ]
        
        try:
            print("\nStarting telemetry components...")
            for p in processes:
                p.start()
                time.sleep(1)  # Staggered startup
                
            print("\nSystem operational - Press Ctrl+C to shutdown")
            print("-------------------------------------------")
            
            # Enhanced process monitoring
            while True:
                dead = [p for p in processes if not p.is_alive()]
                if dead:
                    print(f"CRASHED: {[p.pid for p in dead]}")
                    break
                
                # Debug queue status
                print(f"Queue Status - Export: {export_queue.qsize()}/100, Plot: {plot_queue.qsize()}/500")
                time.sleep(1)
                
        except KeyboardInterrupt:
            print("\nShutdown signal received...")
        finally:
            print("\nTerminating processes...")
            for p in processes:
                if p.is_alive():
                    p.terminate()
                p.join()
            
            print("Closing Cheat Engine...")
            kill_cheat_engine()
            
            print("Cleaning up shared memory...")
            cleanup_shared_memory()
            
            print("\nSystem shutdown complete")

if __name__ == "__main__":
    main()