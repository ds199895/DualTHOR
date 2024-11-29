import unity_launcher  # 导入启动和关闭 Unity 的逻辑
import tcp_server  # 导入 TCP 服务器逻辑

def main():
    """主程序入口：先启动 Unity 然后启动 TCP 服务器。"""
    # 启动 Unity 环境
    unity_process = unity_launcher.start_unity()
    if not unity_process:
        print("Failed to start Unity. Exiting...")
        return

    try:
        # 启动 TCP 服务器进行通信
        tcp_server.start_server()
    except KeyboardInterrupt:
        print("Server stopped by user.")
    finally:
        # 程序退出时关闭 Unity
        unity_launcher.stop_unity(unity_process)

if __name__ == '__main__':
    main()