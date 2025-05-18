import logging
from controller import Controller
import time
import sys
import json

def test_controller():
    # 创建 Controller 实例
    # scene name:
    # kitchen
    # bedroom
    # bathroom
    # livingroom
    # livingroom2
    controller = Controller(config_path="config.json", start_unity_exe=True,robot_type='x1', scene="livingroom2")
    
    # 启动控制器
    controller.start()
    
    logging.info("waiting for scene loading...")
    time.sleep(3)  # 

    # Simple menu for user interaction debugging options
    while True:
        print("\n==== Collision Detection Debugging Menu ====")
        print("1. Small distance move (0.05)")
        print("2. Medium distance move (0.1)")
        print("3. Large distance move (1)")
        print("4. undo")
        print("5. move left (0.05)")
        print("6. move right (0.05)")
        print("7. move back (0.05)")
        print("8. rotate 45 degrees")
        print("9. pick cup")
        print("10. place cup")
        print("11. reset")
        print("0. exit")
        
        try:
            choice = input("please choose action (0-11): ")
            
            if choice == '0':
                logging.info("exit test")
                break
            elif choice == '1':
                feedback = controller.step("moveahead", magnitude=0.05)
                print_feedback_result(feedback, "small distance move")
            elif choice == '2':
                feedback = controller.step("moveahead", magnitude=0.1)
                print_feedback_result(feedback, "medium distance move")
            elif choice == '3':
                feedback = controller.step("moveahead", magnitude=1)
                print_feedback_result(feedback, "large distance move")
            elif choice == '4':
                feedback = controller.step("undo")
                print_feedback_result(feedback, "undo")
            elif choice == '5':
                feedback = controller.step("moveleft", magnitude=0.05)
                print_feedback_result(feedback, "move left")
            elif choice == '6':
                feedback = controller.step("moveright", magnitude=0.05)
                print_feedback_result(feedback, "move right")
            elif choice == '7':
                feedback = controller.step("moveback", magnitude=0.05)
                print_feedback_result(feedback, "move back")
            elif choice == '8':
                feedback = controller.step("rotateright", magnitude=0.5)
                print_feedback_result(feedback, "rotate")
            elif choice == '9':
                feedback = controller.step("pick", objectID="Kitchen_Cup_01", arm="left")
                print_feedback_result(feedback, "pick cup")
            elif choice == '10':
                feedback = controller.step("place", objectID="Kitchen_Cup_01", arm="left")
                print_feedback_result(feedback, "place cup")
            elif choice == '11':
                # feedback = controller.step("reset")
                feedback=controller.reset_environment()
                print_feedback_result(feedback, "reset")
            else:
                logging.warning("invalid choice, please try again")
                
        except Exception as e:
            logging.error(f"operation error: {e}")
    
    logging.info("test end")


def test_dual_arm():
    controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='h1', scene="kitchen")
    controller.start()
    controller.step("rotateright", magnitude=1)
    controller.step("moveahead", magnitude=1.6)
    controller.step("moveleft", magnitude=2)
    
    # example 1: sequential execution - left arm pick bowl, then right arm open drawer
    sequential_actions = [
        {
            "action": "pick",
            "arm": "left",
            "objectID": "Bowl_148b0fbf",
            "successRate": 0.95
        },
        {
            "action": "open",
            "arm": "right",
            "objectID": "Kitchen_Drawer_01",
            "successRate": 0.95
        }
    ]

    # sequential execution (sequential=True)
    results = controller.execute_dual_arm_actions(sequential_actions, sequential=True)

    print(results)


    parallel_actions = [
        {
            "action": "pick",
            "arm": "left",
            "objectID": "Bowl_148b0fbf",
            "successRate": 0.95
        },
        {
            "action": "pick",
            "arm": "right",
            "objectID": "Kitchen_Potato_01",
            "successRate": 0.95
        }
    ]

    # parallel execution (sequential=False)
    # results = controller.execute_dual_arm_actions(parallel_actions, sequential=False)
    # print(results)
    # controller.step("undo")

def print_feedback_result(feedback, action_name):
    """print action execution result, including collision details"""
    success = feedback.get('success', False)
    logging.info(f"{action_name} result: {'success' if success else 'failed'}")
    
    # if there is an error message, print it
    if 'msg' in feedback and feedback['msg']:
        logging.info(f"message: {feedback['msg']}")
    
    # check if there is collision information
    if 'collision_info' in feedback:
        collision = feedback['collision_info']
        logging.info(f"collision details: source={collision.get('source', 'unknown')}, target={collision.get('target', 'unknown')}")
        
    # print the summary of the feedback object (excluding large data)
    reduced_feedback = {k: v for k, v in feedback.items() if k not in ['sceneState']}
    logging.debug(f"full feedback: {json.dumps(reduced_feedback, ensure_ascii=False)}")

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    
    try:
        test_controller()

        # test_dual_arm()

        while True:
            time.sleep(1)

        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        logging.info("user interrupt test")
        sys.exit(0)
    except Exception as e:
        logging.error(f"test error: {e}")
        sys.exit(1)
