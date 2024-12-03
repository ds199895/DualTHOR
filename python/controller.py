import logging
import json
from tcp_server import TCPServer
from actions import Actions

logging.basicConfig(level=logging.INFO)


class Controller:
    def __init__(self, host='localhost', port=5678, config_path="config.json"):
        """
        初始化控制器，包括动作执行器、TCP 服务器和配置加载。
        """
        self.executor = Actions()
        self.tcp_server = TCPServer(host, port)
        self.config = self.load_config(config_path)

    def load_config(self, config_path):
        """
        加载配置文件。
        :param config_path: 配置文件路径
        :return: 配置数据
        """
        try:
            with open(config_path, 'r') as file:
                config = json.load(file)
                logging.info("Config loaded successfully.")
                return config
        except Exception as e:
            logging.error(f"Error loading config: {e}")
            return {}

    def get_default_success_rate(self, action_name):
        """
        从配置文件获取动作的默认成功率。
        """
        success_rates = self.config.get("success_rate", {})
        return success_rates.get(action_name.lower(), 1.0)

    def start(self):
        """启动控制器"""
        try:
            self.tcp_server.start()
            self.handle_user_input()
        except Exception as e:
            logging.error(f"Error in Controller: {e}")
        finally:
            self.tcp_server.stop()

    def step(self, action_name, **kwargs):
        """
        执行动作，并通过 TCP 服务器发送命令。
        """
        try:
            # 如果未指定 success_rate，从配置中加载默认值
            if "successRate" not in kwargs:
                kwargs["successRate"] = self.get_default_success_rate(action_name)

            action_json = self.executor.execute_action(action_name, **kwargs)
            self.tcp_server.send(action_json)
            feedback = self.tcp_server.receive()
            logging.info(f"Feedback from Unity: {feedback}")
            return feedback
        except Exception as e:
            logging.error(f"Error in step execution: {e}")
            return {"error": str(e)}

    def handle_user_input(self):
        """
        动态解析用户输入，构造参数并调用动作执行器。
        """
        while True:
            try:
                user_input = input("Enter action and parameters: ").strip()
                if not user_input:
                    logging.warning("Empty input. Please enter a valid action.")
                    continue

                # 动态解析输入
                parts = user_input.split()
                action_name = parts[0].lower()
                parameters = self.executor.parse_parameters(parts[1:])

                # 执行动作
                feedback = self.step(action_name, **parameters)
                # logging.info(f"Feedback: {feedback}")
            except KeyboardInterrupt:
                logging.info("User stopped the program.")
                break
            except Exception as e:
                logging.error(f"Error handling user input: {e}")
    
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
    controller = Controller(config_path="config.json")  
    controller.start()