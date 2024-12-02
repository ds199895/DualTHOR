import threading
import server_ik  # 导入 ikserver.py
import unity_launcher  # Unity 启动逻辑
import tcp_server  # TCP 服务器逻辑

def main():
    """通过 main 启动 IK 服务、Unity 和 TCP 服务"""
    # 启动 Unity 环境
    unity_process = unity_launcher.start_unity()
    if not unity_process:
        print("Failed to start Unity. Exiting...")
        return

    # 启动 IK 服务
    ik_thread = threading.Thread(target=server_ik.start_ik_server, daemon=True)
    ik_thread.start()
    print("IK server started.")

    try:
        # 启动 TCP 服务器
        tcp_server.start_server()
    except KeyboardInterrupt:
        print("Server stopped by user.")
    finally:
        # 停止 Unity 环境
        unity_launcher.stop_unity(unity_process)
        print("IK server stopped.")

if __name__ == '__main__':
    main()