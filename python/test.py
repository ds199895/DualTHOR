import logging
from controller import Controller
import time

def test_controller():
    # 创建 Controller 实例
    # scene name:
    # kitchen
    # bedroom
    # bathroom
    # livingroom
    # livingroom2
    controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='h1', scene="livingroom2")
    
    # 启动控制器
    controller.start()
    

    cur_state=controller.step("getcurstate")

    print("cur state: ",cur_state)

    # 测试move
    logging.info("Testing move...")
    controller.step("tp",objectID="LivingRoom_Bottle_01")

    json_actions = '[{"action":"pick","arm":"left","objectID":"LivingRoom_Bottle_01"}, {"action":"pick","arm":"right","objectID":"LivingRoom_Bottle_02"}]'



    controller.step_async(json_actions)
    # feedback=controller.step("pick",arm="left",objectID="LivingRoom_Bottle_01")

    # feedback=controller.step("moveright",magnitude=1)




    

    # 保持运行
    while True:
        time.sleep(1)


   

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    test_controller()
