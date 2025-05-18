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
        initialize controller, including action executor, TCP server and config loading.
        """
        self.executor = Actions()
        self.tcp_server = TCPServer(host, port)
        self.config = self.load_config(config_path)
        self.thread_pool = ThreadPoolExecutor(max_workers=10)  # max number of concurrent threads
        self.stop_event = threading.Event()
        self.robot_type = robot_type  # save robot_type
        self.tcp_server.on_connect = self.on_client_connect  # set connection event callback
        self.scene=scene
        self.feedback_queue = queue.Queue()  # for storing feedback
        self.last_collision_info = None  # store last collision info


        # if need to start Unity executable file
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
        # start IK server
        ik_thread = threading.Thread(target=server_ik.start_server_ik, daemon=True)
        ik_thread.start()
        print("IK server started.")



    def load_config(self, config_path):
        """
        load config file.
        :param config_path: config file path
        :return: config data
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
        get default success rate from config file.
        """
        success_rates = self.config.get("success_rate", {})
        return success_rates.get(action_name.lower(), 1.0)

    def start(self):
        """start controller"""
        try:
            self.tcp_server.start()
            # start feedback receiving thread
            # threading.Thread(target=self.handle_feedback, daemon=True).start()
            # start user input processing
            # self.handle_user_input()
        except Exception as e:
            logging.error(f"Error in Controller: {e}")
        # finally:
        #     self.stop_event.set()
        #     self.tcp_server.stop()
        #     self.thread_pool.shutdown(wait=True)

    def step(self, action_name, **kwargs):
        """
        execute action, send command through TCP server, wait and return feedback.
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
        # # submit action to thread pool, execute independently, and wait for result
        # future = self.thread_pool.submit(execute_action)
        # return future.result()  # wait for execute_action to complete and return result
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
            
            # process collision info
            self._process_collision_info(feedback_json)
            
            # output detailed information in log
            if not feedback_json.get('success', False):
                logging.warning(f"action '{action_name}' failed: {feedback_json.get('msg', 'unknown error')}")
                if self.last_collision_info:
                    logging.warning(f"collision info: {self.last_collision_info}")
            
            return feedback_json
        except Exception as e:
            logging.error(f"Error executing action {action_name}: {e}")
            return None
        
    def _process_collision_info(self, feedback_json):
        """
        extract and process collision info from feedback
        """
        # clear last collision info
        self.last_collision_info = None
        
        # check if there is collision info
        if 'collision_info' in feedback_json:
            collision_info = feedback_json['collision_info']
            self.last_collision_info = collision_info
            
            source = collision_info.get('source', 'unknown')
            target = collision_info.get('target', 'unknown')
            
            # record detailed collision info
            logging.info(f"detected collision: {source} and {target}")
            
    def get_last_collision_info(self):
        """
        get last collision info
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
            
            # process collision info
            self._process_collision_info(feedback_json)
            
            # check if it is multi-action feedback (contains results field)
            if 'results' in feedback_json:
                # record execution result of each action
                for result in feedback_json['results']:
                    action_name = result.get('action', 'unknown action')
                    arm = result.get('arm', 'unknown arm')
                    success = result.get('success', False)
                    msg = result.get('msg', 'no message')
                    logging.info(f"dual arm action result: {arm} arm, {action_name}, success: {success}, msg: {msg}")
            
            return feedback_json
        except Exception as e:
            logging.error(f"Error executing action {actions_json}: {e}")
            return None

    def handle_feedback(self):
        """
        background thread: handle feedback from Unity
        """
        while not self.stop_event.is_set():
            try:
                feedback = self.tcp_server.receive()
                logging.info(f"Feedback from Unity: {feedback}")
                self.feedback_queue.put(feedback)  # put feedback into queue
            except Exception as e:
                logging.error(f"Error receiving feedback: {e}")

    def handle_user_input(self):
        """
        dynamic parsing user input, construct parameters and call action executor.
        """
        while True:
            try:
                user_input = input("Enter action and parameters: ").strip()
                if not user_input:
                    logging.warning("Empty input. Please enter a valid action.")
                    continue

                # dynamic parsing input
                parts = user_input.split()
                action_name = parts[0].lower()
                parameters = self.executor.parse_parameters(parts[1:])

                # execute action immediately
                self.step(action_name, **parameters)
            except KeyboardInterrupt:
                logging.info("User stopped the program.")
                break
            except Exception as e:
                logging.error(f"Error handling user input: {e}")

    def reset_environment(self):
        """
        reset environment.
        """
        logging.info("Resetting environment...")
        self.step("resetstate")

    def undo_last_action(self):
        """
        undo last action.
        """
        logging.info("Undoing last action...")
        self.step("undo")

    def redo_last_action(self):
        """
        redo last action.
        """
        logging.info("Redoing last action...")
        self.step("redo")

    def load_robot(self, robottype):
        """
        load specified type of robot.
        """
        logging.info(f"Loading robot of type: {robottype}")
        self.step("loadrobot", robottype=robottype)
    
    def reset_scene(self,scene,robottype):
        logging.info(f"Loading scene: {scene}")
        return self.step("resetscene",scene=scene,robottype=robottype)


    def on_client_connect(self):
        """
        event triggered when client connects.
        """
        logging.info(f"Client connected, sending loadrobot command for robot type: {self.robot_type}.")
        # res=self.reset_scene(scene=self.scene,robottype=self.robot_type)
        res=self.step("resetscene",scene=self.scene,robottype=self.robot_type)


        print("reset scene feed back: ",res)
        if res['success']:
            self.load_robot(self.robot_type)


    def wait_for_signal(self, signal_name):
        """
        wait for specific signal.
        """
        logging.info(f"Waiting for signal: {signal_name}")
        while True:
            feedback = self.tcp_server.receive()
            if signal_name in feedback:
                logging.info(f"Received signal: {signal_name}")
                break
            time.sleep(1)  # delay to avoid excessive CPU usage

    def execute_dual_arm_actions(self, actions, sequential=False):
        """
        execute dual arm actions, support synchronous and sequential modes
        
        parameters:
        - actions: list of actions, each action is a dictionary
        - sequential: whether to execute sequentially (True=sequential, False=parallel)
        
        return:
        - dictionary containing execution results of two arms
        """
        # ensure actions is a list
        if not isinstance(actions, list):
            logging.error("Actions must be a list")
            return {"success": False, "msg": "Actions must be a list", "results": []}
            
        # ensure each action has arm field
        for i, action in enumerate(actions):
            if 'arm' not in action:
                logging.warning(f"action #{i} does not specify arm field, default to 'left'")
                action['arm'] = 'left'
                
      
        execution_mode = "sequential" if sequential else "parallel"
        logging.info(f"set dual arm execution mode to: {execution_mode}")
        dual_arm_actions = {"actions":actions,"executionMode":execution_mode}

        # build action JSON array
        actions_json = json.dumps(dual_arm_actions)
        logging.info(f"execute dual arm actions: mode={'sequential' if sequential else 'parallel'}, action number={len(actions)}")
        
        # send action and get feedback
        feedback = self.step_async(actions_json)
        
        if not feedback:
            return {"success": False, "msg": "failed to execute dual arm actions, no feedback", "results": []}
            
        # extract execution results of each arm
        results = feedback.get('results', [])
        all_success = all(result.get('success', False) for result in results) if results else False
        
        return {
            "success": all_success,
            "msg": "dual arm actions completed",
            "results": results
        }

# start controller
if __name__ == '__main__':
    controller = Controller(config_path="config.json")  
    controller.start()

    # dual arm actions example
    def dual_arm_example():
        """
        dual arm actions example
        """
        print("\n=== dual arm actions example ===")
        
        # create controller instance
        controller = Controller(config_path="config.json", start_unity_exe=False)
        controller.start()
        
        # wait for connection
        time.sleep(2)
        
        # example 1: sequential execution - left arm pick cup, then right arm open fridge
        print("\n1. sequential execution example (left arm pick cup, then right arm open fridge)")
        sequential_actions = [
            {
                "action": "pick",
                "arm": "left",
                "objectID": "Cup_1",
                "successRate": 0.95
            },
            {
                "action": "open",
                "arm": "right",
                "objectID": "Fridge_1",
                "successRate": 0.95
            }
        ]
        
        results = controller.execute_dual_arm_actions(sequential_actions, sequential=True)
        print(f"sequential execution result: success={results['success']}")
        for i, result in enumerate(results.get('results', [])):
            print(f"   action {i+1} ({result.get('arm')} arm {result.get('action')}): "
                  f"{'success' if result.get('success') else 'failed'} - {result.get('msg')}")
        
        # wait for a few seconds
        time.sleep(3)
        
        # example 2: parallel execution - two arms place object
        print("\n2. parallel execution example (two arms place object)")
        parallel_actions = [
            {
                "action": "place",
                "arm": "left", 
                "objectID": "Cup_1",
                "successRate": 0.95
            },
            {
                "action": "place",
                "arm": "right",
                "objectID": "Bottle_1", 
                "successRate": 0.95
            }
        ]
        
        results = controller.execute_dual_arm_actions(parallel_actions, sequential=False)
        print(f"parallel execution result: success={results['success']}")
        for i, result in enumerate(results.get('results', [])):
            print(f"   action {i+1} ({result.get('arm')} arm {result.get('action')}): "
                  f"{'success' if result.get('success') else 'failed'} - {result.get('msg')}")
            
    # uncomment the following line to run the example
    # dual_arm_example()