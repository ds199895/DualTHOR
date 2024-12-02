import logging
from actions import Actions
from tcp_server import TCPServer
import json

logging.basicConfig(level=logging.INFO)

class Controller:
    def __init__(self, host='localhost', port=5678, config_path="config.json"):
        """
        初始化控制器，包括动作执行器和 TCP 服务器。
        """
        self.executor = Actions()
        self.tcp_server = TCPServer(host, port)
        self.success_rates = self.load_config(config_path)  # 加载配置文件中的成功率

    def load_config(self, file_path):
        """
        加载 config.json 文件，并提取成功率配置。
        """
        try:
            logging.info(f"Loading config from: {file_path}")
            with open(file_path, 'r') as file:
                config = json.load(file)
                logging.info(f"Config loaded: {config}")
                return config.get("success_rate", {})
        except Exception as e:
            logging.error(f"Error loading config: {e}")
            return {}
        
    def start(self):
        """
        启动服务器并处理用户输入。
        """
        try:
            self.tcp_server.start()  # 启动 TCP 服务器
            self.handle_user_input()  # 处理用户输入
        except Exception as e:
            logging.error(f"Error in Controller: {e}")
        finally:
            self.tcp_server.stop()  # 停止服务器
            
    def step(self, action, moveMagnitude=None, successRate=None):
        """
        执行一个动作 (AI2-THOR 风格接口)。
        """
        if moveMagnitude is None:
            moveMagnitude = 1.0

        # 使用配置文件中的默认成功率
        if successRate is None:
            successRate = self.success_rates.get(action.lower(), 1.0)
            if successRate == 1.0:
                logging.warning(f"Using default success rate for action: {action}")

        try:
            action_json = self.executor.execute_action(action, moveMagnitude, successRate)
            self.tcp_server.send(action_json)

            feedback = self.tcp_server.receive()
            logging.info(f"Feedback from Unity: {feedback}")
            return feedback
        except Exception as e:
            logging.error(f"Error in step execution: {e}")
            return {"error": str(e)}
        
    def handle_user_input(self):
        """
        处理用户输入并与 Unity 环境交互。
        """
        while True:
            try:
                # 获取用户输入
                action_input = input("Enter action: ").strip()
                if not action_input:
                    logging.warning("Empty action. Please enter a valid action.")
                    continue

                # 解析动作名称
                parts = action_input.split()
                action_name = parts[0]
                move_magnitude = float(parts[1]) if len(parts) > 1 else 1.0

                # 如果用户未输入成功率，将其设置为 None
                success_rate = float(parts[2]) if len(parts) > 2 else None

                # 执行动作
                feedback = self.step(action=action_name, moveMagnitude=move_magnitude, successRate=success_rate)
                print(f"Feedback: {feedback}")

            except KeyboardInterrupt:
                logging.info("User stopped the program.")
                break
            except Exception as e:
                logging.error(f"Error during action handling: {e}")

    def reset_environment(self):
        """
        重置环境。
        """
        logging.info("Resetting environment...")
        reset_action = self.executor.execute_action("reset")
        self.tcp_server.send(reset_action)
        feedback = self.tcp_server.receive()
        logging.info(f"Reset feedback: {feedback}")

    def undo_last_action(self):
        """
        撤销上一个动作。
        """
        logging.info("Undoing last action...")
        undo_action = self.executor.execute_action("undo")
        self.tcp_server.send(undo_action)
        feedback = self.tcp_server.receive()
        logging.info(f"Undo feedback: {feedback}")

    def redo_last_action(self):
        """
        重做上一个动作。
        """
        logging.info("Redoing last action...")
        redo_action = self.executor.execute_action("redo")
        self.tcp_server.send(redo_action)
        feedback = self.tcp_server.receive()
        logging.info(f"Redo feedback: {feedback}")


# 启动控制器
if __name__ == '__main__':
    controller = Controller(config_path="config.json")  # 指定配置文件路径
    controller.start()