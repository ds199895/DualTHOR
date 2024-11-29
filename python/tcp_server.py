import json
import socket
import logging
from actions import Actions
import unity_launcher
from threading import Thread, Lock
import re

# 设置日志记录
logging.basicConfig(level=logging.INFO)

HOST = 'localhost'
PORT = 5678

# 定义一个锁，用于同步日志输出和输入提示
print_lock = Lock()

def print_prompt():
    """
    清除当前行并重新打印输入提示，使其始终位于最后一行。
    """
    print("\033[F\033[K", end='')  # ANSI 控制码：上移一行并清除
    print(f"Enter action to send to Unity (MoveAhead, MoveRight, MoveBack, MoveLeft, RotateRight, RotateLeft, Pick Left/Right, Place Left/Right, ResetJoint Left/Right, Undo, Redo,tp Kitchen_Cup_01,Pick Left Kitchen_Cup_01,Place Left Kitchen_Cup_01,Toggle Kitchen_Faucet_01, Open Kitchen_Fridge_01，ObjectId: Kitchen_StoveKnob_01, Kitchen_Mug_01, Kitchen_Pan_01, Kitchen_Cabinet_02, Kitchen_Mug_02, Kitchen_Potato_01, Kitchen_Faucet_01, Kitchen_StoveKnob_02, Kitchen_Cup_01, Kitchen_Fridge_01, Kitchen_Drawer_01, Kitchen_Drawer_03, Kitchen_PaperTowelRoll_01, Kitchen_StoveKnob_04, Kitchen_StoveKnob_03, Kitchen_CoffeeMachine_01, Kitchen_Drawer_02, Kitchen_Cabinet_01): ", end='', flush=True)
        
def process_action(action_to_unity, conn, executor):
    try:
        # 将用户输入传递给 executor，供动作方法解析
        executor.set_current_input(action_to_unity)

        # 提取动作名称和可选参数
        parts = action_to_unity.split()
        action_name = parts[0].lower() if len(parts) > 0 else ""
        move_magnitude = float(parts[1]) if len(parts) > 1 and parts[1].isdigit() else 1.0
        success_rate = float(parts[2]) if len(parts) > 2 and parts[2].replace('.', '', 1).isdigit() else 1.0

        # 执行动作并获取返回的 JSON
        action_json = executor.execute_action(action_name, move_magnitude,success_rate)

        # 将 JSON 发送到 Unity
        conn.sendall(action_json.encode())
        with print_lock:
            logging.info(f"Action sent to Unity: {action_json}")
            print_prompt()

        # 接收来自 Unity 的反馈
        data_buffer = b''
        while True:
            chunk = conn.recv(1024)
            if not chunk:
                with print_lock:
                    logging.warning("No response from Unity, closing connection.")
                    print_prompt()
                break
            data_buffer += chunk
            if data_buffer.endswith(b'\n'):
                break

        # 处理反馈数据
        if data_buffer:
            received_data = data_buffer.decode()
            with print_lock:
                logging.info(f"Received feedback from Unity: {received_data}")
                print_prompt()
        else:
            with print_lock:
                logging.warning("Empty data received, closing connection.")
                print_prompt()

    except Exception as e:
        with print_lock:
            logging.error(f"Error processing action: {e}")
            print_prompt()

def handle_client(conn, addr, executor):
    """
    处理客户端连接并与 Unity 进行通信。
    """
    logging.info(f"Connected by {addr}")

    conn.settimeout(60.0)

    try:
        while True:
            with print_lock:
                print_prompt()  # 每次等待用户输入前刷新提示

            action_to_unity = input()  # 将输入单独拿出来，避免提示行干扰
            
            if not action_to_unity:
                with print_lock:
                    logging.warning("No action entered, please enter a valid action.")
                    print_prompt()
                continue

            # 每个指令启动一个新线程
            thread = Thread(target=process_action, args=(action_to_unity, conn, executor))
            thread.start()

    except Exception as e:
        with print_lock:
            logging.error(f"Error handling client {addr}: {e}")
    finally:
        conn.close()
        with print_lock:
            logging.info(f"Connection with {addr} closed.")
            print_prompt()

def start_server(start_unity=False):
    """
    启动 TCP 服务器，并可选启动 Unity .exe 文件。

    参数:
    - start_unity (bool): 是否启动 Unity 应用程序 (.exe 文件)，默认不启动。
    """
    executor = Actions()  # 初始化动作执行器

    # 可选启动 Unity .exe 文件
    unity_process = None
    if start_unity:
        unity_process = unity_launcher.start_unity()  # 启动 Unity .exe
        if not unity_process:
            logging.error("Failed to start Unity. Exiting...")
            return

    # 创建 TCP 服务器
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((HOST, PORT))
        server_socket.listen()
        logging.info(f"Server listening on {HOST}:{PORT}")

        try:
            conn, addr = server_socket.accept()
            handle_client(conn, addr, executor)  # 直接调用 handle_client 处理连接
        except KeyboardInterrupt:
            logging.info("\nServer stopped by user.")
        finally:
            if start_unity and unity_process:
                unity_launcher.stop_unity(unity_process)  # 停止 Unity .exe
            logging.info("Server shutting down.")

if __name__ == '__main__':
    # 根据需求设置 start_unity=True 或 False
    start_server(start_unity=False)  # False 表示只启动 TCP 服务器进行调试，不启动 .exe