import logging
from controller import Controller

def test_controller():
    # 创建 Controller 实例
    controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='h1', scene="livingroom2")
    
    # 启动控制器
    controller.start()
    
    # # 测试加载机器人
    # logging.info("Testing load_robot...")
    # controller.load_robot('X1')
    
    # 测试重置场景
    # logging.info("Testing reset_scene...")
    # # controller.reset_scene('livingroom2', 'h1')

    # controller.step('resetscene',scene="living_room2",robottype="h1")

    cur_state=controller.step("getcurstate")

    # print("cur state: ",cur_state)

    # 测试move
    logging.info("Testing move...")
    controller.step("tp",objectID="LivingRoom_Bottle_01")

    feedback=controller.step("pick",arm="left",objectID="LivingRoom_Bottle_01")

    feedback=controller.step("moveright",magnitude=1)


    
    

    # logging.info("Testing undo")

    # if(not feedback['success']):
    #     feedback=controller.step("undo")
    

    print("feedback res",feedback);



    # # 测试撤销动作
    # logging.info("Testing undo_last_action...")
    # controller.undo_last_action()
    
    # # 测试重做动作
    # logging.info("Testing redo_last_action...")
    # controller.redo_last_action()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    test_controller()
