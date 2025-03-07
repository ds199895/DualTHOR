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
    controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='h1', scene="kitchen")
    
    # 启动控制器
    controller.start()
    

    cur_state=controller.step("getcurstate")

    print("cur state: ",cur_state)

    # 测试move
    logging.info("Testing move...")

    # controller.step("rotateright",magnitude=1)

    # controller.step("moveahead",magnitude=1.5)

    controller.step("tp",objectID="Kitchen_Faucet_01")

    controller.step("toggle",arm="left",objectID="Kitchen_Faucet_01")
    time.sleep(10)
    controller.step("toggle",objectID="Kitchen_Faucet_01")

    # controller.step("toggle",objectID="Kitchen_CoffeeMachine_01")
    # time.sleep(10)
    # controller.step("toggle",objectID="Kitchen_CoffeeMachine_01")
    
    # controller.step("resetpose")

    # controller.step("pick", arm="left",objectID="Kitchen_Mug_02")

    # controller.step("moveleft", magnitude=0.2)

    # controller.step("tp",objectID="Kitchen_Mug_01")

    # json_actions = '[{"action":"pick","arm":"left","objectID":"LivingRoom_Bottle_01"}, {"action":"pick","arm":"right","objectID":"LivingRoom_Bottle_02"}]'



    # controller.step_async(json_actions)
    # feedback=controller.step("pick",arm="left",objectID="Kitchen_Mug_01")

    # feedback=controller.step("moveright",magnitude=1)




    

    # 保持运行
    while True:
        time.sleep(1)


   

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    test_controller()
