import threading
import server_ik  
import unity_launcher  
from controller import Controller  

def main(start_unity_exe=True):
    """
    启动程序入口。
    :param start_unity_exe: 是否启动 Unity 可执行文件
    """
    # 如果需要启动 Unity 可执行文件
    unity_process = None
    if start_unity_exe:
        unity_process = unity_launcher.start_unity()
        if not unity_process:
            print("Failed to start Unity. Exiting...")
            return
        print("Unity executable started.")
    else:
        print("Skipping Unity executable startup. Using Unity Editor for communication.")

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
        # 如果启动了 Unity 可执行文件，则关闭它
        if unity_process:
            unity_launcher.stop_unity(unity_process)
            print("Unity environment stopped.")


if __name__ == '__main__':
    # 这里可以根据需求设置是否启动 Unity 可执行文件,仅调试时设置为 False！！！
    main(start_unity_exe=False)