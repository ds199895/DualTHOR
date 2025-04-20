import logging
import json
from tcp_server import TCPServer
from actions import Actions
from concurrent.futures import ThreadPoolExecutor, as_completed
import threading
import time
import queue
logging.basicConfig(level=logging.INFO)
import server_ik_x1
import server_ik_h1 
import unity_launcher  
class Controller:
    def __init__(self, host='localhost', port=5678, config_path="config.json", start_unity_exe=True, robot_type='X1',scene="livingroom2"):
        """
        初始化控制器，包括动作执行器、TCP 服务器和配置加载。
        """
        self.executor = Actions()
        self.tcp_server = TCPServer(host, port)
        self.config = self.load_config(config_path)
        self.thread_pool = ThreadPoolExecutor(max_workers=10)  # 最大并发线程数
        self.stop_event = threading.Event()
        self.robot_type = robot_type  # 保存 robot_type
        self.tcp_server.on_connect = self.on_client_connect  # 设置连接事件回调
        self.scene=scene
        self.feedback_queue = queue.Queue()  # 用于存储反馈的队列
        self.last_collision_info = None  # 存储最后一次碰撞信息


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
        

        if robot_type=="x1":
            server_ik=server_ik_x1
        elif robot_type=="h1":
            server_ik=server_ik_h1
        print(server_ik)
        # 启动 IK 服务
        ik_thread = threading.Thread(target=server_ik.start_server_ik, daemon=True)
        ik_thread.start()
        print("IK server started.")



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
            # 启动反馈接收线程
            # threading.Thread(target=self.handle_feedback, daemon=True).start()
            # 启动用户输入处理
            # self.handle_user_input()
        except Exception as e:
            logging.error(f"Error in Controller: {e}")
        # finally:
        #     self.stop_event.set()
        #     self.tcp_server.stop()
        #     self.thread_pool.shutdown(wait=True)

    def step(self, action_name, **kwargs):
        """
        执行动作，并通过 TCP 服务器发送命令，等待并返回反馈。
        """
        def execute_action():
            try:
                if "successRate" not in kwargs:
                    kwargs["successRate"] = self.get_default_success_rate(action_name)

                action_json = self.executor.execute_action(action_name, **kwargs)
                self.tcp_server.send(action_json)

                logging.info(f"Action '{action_name}' sent with parameters: {kwargs}")

                feed_back = None
                while feed_back is None:
                    print("feedback: ", feed_back)
                    feed_back = self.feedback_queue.get()
                # print("result feed back: ", feed_back)
                feedback_json=json.loads(feed_back)
                # print("feed back json: ", feedback_json)
                
                return feedback_json
            except Exception as e:
                logging.error(f"Error executing action {action_name}: {e}")
                return None
        # self.thread_pool.submit(execute_action)
        # # 将动作提交到线程池，独立执行，并等待结果
        # future = self.thread_pool.submit(execute_action)
        # return future.result()  # 等待 execute_action 完成并返回结果
        # execute_action()
      
        if "successRate" not in kwargs:
            kwargs["successRate"] = self.get_default_success_rate(action_name)

        action_json = self.executor.execute_action(action_name, **kwargs)
        self.tcp_server.send(action_json)

        logging.info(f"Action '{action_name}' sent with parameters: {kwargs}")
        try:
            feed_back=self.tcp_server.receive()
            # print("feedback string: ",feed_back)
            feedback_json=json.loads(feed_back)
            
            # 处理碰撞信息
            self._process_collision_info(feedback_json)
            
            # 在日志中输出详细信息
            if not feedback_json.get('success', False):
                logging.warning(f"动作 '{action_name}' 执行失败: {feedback_json.get('msg', '未知错误')}")
                if self.last_collision_info:
                    logging.warning(f"碰撞信息: {self.last_collision_info}")
            
            return feedback_json
        except Exception as e:
            logging.error(f"Error executing action {action_name}: {e}")
            return None
        
    def _process_collision_info(self, feedback_json):
        """
        从反馈中提取并处理碰撞信息
        """
        # 清除上一次的碰撞信息
        self.last_collision_info = None
        
        # 检查是否有碰撞信息
        if 'collision_info' in feedback_json:
            collision_info = feedback_json['collision_info']
            self.last_collision_info = collision_info
            
            source = collision_info.get('source', 'unknown')
            target = collision_info.get('target', 'unknown')
            
            # 记录详细的碰撞信息
            logging.info(f"检测到碰撞: {source} 与 {target}")
            
    def get_last_collision_info(self):
        """
        获取最后一次碰撞信息
        """
        return self.last_collision_info
        
    def step_async(self,actions_json):
        # if "successRate" not in kwargs:
        #     kwargs["successRate"] = self.get_default_success_rate(action_name)

        # action_json = self.executor.execute_action(action_name, **kwargs)
        self.tcp_server.send(actions_json)

        # logging.info(f"Action '{action_name}' sent with parameters: {kwargs}")
        try:
            feed_back=self.tcp_server.receive()
            # print("feedback string: ",feed_back)
            feedback_json=json.loads(feed_back)
            
            # 处理碰撞信息
            self._process_collision_info(feedback_json)
            
            return feedback_json
        except Exception as e:
            logging.error(f"Error executing action {actions_json}: {e}")
            return None

    def handle_feedback(self):
        """
        后台线程：统一处理来自 Unity 的反馈。
        """
        while not self.stop_event.is_set():
            try:
                feedback = self.tcp_server.receive()
                logging.info(f"Feedback from Unity: {feedback}")
                self.feedback_queue.put(feedback)  # 将反馈放入队列
            except Exception as e:
                logging.error(f"Error receiving feedback: {e}")

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

                # 立即执行动作
                self.step(action_name, **parameters)
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
        self.step("resetstate")

    def undo_last_action(self):
        """
        撤销上一个动作。
        """
        logging.info("Undoing last action...")
        self.step("undo")

    def redo_last_action(self):
        """
        重做上一个动作。
        """
        logging.info("Redoing last action...")
        self.step("redo")

    def load_robot(self, robottype):
        """
        加载指定类型的机器人。
        """
        logging.info(f"Loading robot of type: {robottype}")
        self.step("loadrobot", robottype=robottype)
    
    def reset_scene(self,scene,robottype):
        logging.info(f"Loading scene: {scene}")
        return self.step("resetscene",scene=scene,robottype=robottype)


    def on_client_connect(self):
        """
        客户端连接时触发的事件。
        """
        logging.info(f"Client connected, sending loadrobot command for robot type: {self.robot_type}.")
        # res=self.reset_scene(scene=self.scene,robottype=self.robot_type)
        res=self.step("resetscene",scene=self.scene,robottype=self.robot_type)


        print("reset scene feed back: ",res)
        if res['success']:
            self.load_robot(self.robot_type)


    def wait_for_signal(self, signal_name):
        """
        等待特定信号的到来。
        """
        logging.info(f"Waiting for signal: {signal_name}")
        while True:
            feedback = self.tcp_server.receive()
            if signal_name in feedback:
                logging.info(f"Received signal: {signal_name}")
                break
            time.sleep(1)  # 延迟以避免过多的CPU占用

# 启动控制器
if __name__ == '__main__':
    controller = Controller(config_path="config.json")  
    controller.start()