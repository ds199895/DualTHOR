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
    controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='x1', scene="bedroom")
    
    # 启动控制器
    controller.start()
    
    logging.info("等待场景加载完成...")
    time.sleep(3)  # 给更多时间让场景加载完成
    
    # 测试基本移动，检查碰撞检测是否工作
    logging.info("===== 测试极小距离移动 =====")
    
    # 测试几个极小的移动幅度
    # test_magnitudes = [0.005, 0.01, 0.02, 0.03]
    # for mag in test_magnitudes:
    #     logging.info(f"测试极小移动: {mag}")
    #     feedback = controller.step("moveahead", magnitude=mag)
    #     logging.info(f"移动结果 [{mag}]: {feedback.get('success', False)}")
        
    #     # 检查是否有碰撞信息
    #     if 'collision_info' in feedback:
    #         collision_info = feedback['collision_info']
    #         logging.info(f"碰撞详情: 源={collision_info.get('source', '未知')}, 目标={collision_info.get('target', '未知')}")
            
    #     time.sleep(0.5)
    


    # 简单菜单提供用户交互调试选项
    while True:
        print("\n==== 碰撞检测调试菜单 ====")
        print("1. 小距离前进 (0.05)")
        print("2. 中等距离前进 (0.1)")
        print("3. 大距离前进 (1)")
        print("4. undo")
        print("5. 向左移动 (0.05)")
        print("6. 向右移动 (0.05)")
        print("7. 向后移动 (0.05)")
        print("8. 旋转45度")
        print("9. 拿起杯子")
        print("10. 放下杯子")
        print("11. reset")
        print("0. 退出")
        
        try:
            choice = input("请选择操作 (0-11): ")
            
            if choice == '0':
                logging.info("退出测试")
                break
            elif choice == '1':
                feedback = controller.step("moveahead", magnitude=0.05)
                print_feedback_result(feedback, "小距离前进")
            elif choice == '2':
                feedback = controller.step("moveahead", magnitude=0.1)
                print_feedback_result(feedback, "中等距离前进")
            elif choice == '3':
                feedback = controller.step("moveahead", magnitude=1)
                print_feedback_result(feedback, "大距离前进")
            elif choice == '4':
                feedback = controller.step("undo")
                print_feedback_result(feedback, "undo")
            elif choice == '5':
                feedback = controller.step("moveleft", magnitude=0.05)
                print_feedback_result(feedback, "向左移动")
            elif choice == '6':
                feedback = controller.step("moveright", magnitude=0.05)
                print_feedback_result(feedback, "向右移动")
            elif choice == '7':
                feedback = controller.step("moveback", magnitude=0.05)
                print_feedback_result(feedback, "向后移动")
            elif choice == '8':
                feedback = controller.step("rotateright", magnitude=0.5)
                print_feedback_result(feedback, "旋转")
            elif choice == '9':
                feedback = controller.step("pick", objectID="Kitchen_Cup_01", arm="left")
                print_feedback_result(feedback, "拿起杯子")
            elif choice == '10':
                feedback = controller.step("place", objectID="Kitchen_Cup_01", arm="left")
                print_feedback_result(feedback, "放下杯子")
            elif choice == '11':
                # feedback = controller.step("reset")
                feedback=controller.reset_environment()
                print_feedback_result(feedback, "reset")
            else:
                logging.warning("无效选择，请重试")
                
        except Exception as e:
            logging.error(f"操作发生错误: {e}")
    
    logging.info("测试结束")


def test_lift():
    controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='h1', scene="kitchen")
    controller.start()
    # controller.step("rotateright", magnitude=1)
    # controller.step("moveahead", magnitude=1.2)
    # controller.step("moveright", magnitude=0.7)
    controller.step("pick", objectID="Kitchen_Cup_01", arm="left")
    controller.step("place", objectID="Kitchen_Cup_01", arm="left")
    # controller.step("lift",objectID="Kitchen_CoffeeMachine_01")


def test_dual_arm():
    controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='h1', scene="kitchen")
    controller.start()

    # 测试 Lift

    controller.step("rotateright", magnitude=1)
    controller.step("moveahead", magnitude=1.6)
    controller.step("moveleft", magnitude=2)
    
    # 示例1：顺序执行 - 左臂先拿杯子，然后右臂开抽屉
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

    # 顺序执行动作（sequential=True）
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

    # 并行执行动作（sequential=False）
    # results = controller.execute_dual_arm_actions(parallel_actions, sequential=False)
    # print(results)
    # controller.step("undo")

def print_feedback_result(feedback, action_name):
    """打印动作执行结果，包含碰撞详情"""
    success = feedback.get('success', False)
    logging.info(f"{action_name}结果: {'成功' if success else '失败'}")
    
    # 如果有错误消息，打印出来
    if 'msg' in feedback and feedback['msg']:
        logging.info(f"消息: {feedback['msg']}")
    
    # 检查是否有碰撞信息
    if 'collision_info' in feedback:
        collision = feedback['collision_info']
        logging.info(f"碰撞详情: 源={collision.get('source', '未知')}, 目标={collision.get('target', '未知')}")
        
    # 打印整个反馈对象的摘要（排除过大的数据）
    reduced_feedback = {k: v for k, v in feedback.items() if k not in ['sceneState']}
    logging.debug(f"完整反馈: {json.dumps(reduced_feedback, ensure_ascii=False)}")

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    
    try:
        # test_controller()
        test_lift()
        # test_dual_arm()

        while True:
            time.sleep(1)

        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        logging.info("用户中断测试")
        sys.exit(0)
    except Exception as e:
        logging.error(f"测试过程中发生错误: {e}")
        sys.exit(1)
