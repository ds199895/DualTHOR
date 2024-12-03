import threading
import server_ik  
import unity_launcher  
from controller import Controller  

def main():
    """通过 main 启动 IK 服务、Unity 和 Controller"""
    # 启动 Unity 环境
    unity_process = unity_launcher.start_unity()
    if not unity_process:
        print("Failed to start Unity. Exiting...")
        return

    # 启动 IK 服务
    ik_thread = threading.Thread(target=server_ik.start_server_ik, daemon=True)
    ik_thread.start()
    print("IK server started.")

    try:
        # 启动 Controller（代替 TCP 服务器）
        controller = Controller()
        controller.start()
    except KeyboardInterrupt:
        print("Server stopped by user.")
    finally:
        # 停止 Unity 环境
        unity_launcher.stop_unity(unity_process)
        print("Unity environment stopped.")

if __name__ == '__main__':
    main()