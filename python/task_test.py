import logging
from controller import Controller
import time
import json
import tqdm

def test_controller():

    json_file = f'./task_list.json'
    with open(json_file, 'r') as file:
        data = json.load(file)

    # print(data)
    success_rate = 0
    num_test = 10

    controller = Controller(config_path="config.json", start_unity_exe=True,robot_type='h1', scene="kitchen")
    
    controller.start()

    # controller.step("tp",objectID="Kitchen_CoffeeMachine_01")
    # feed_back = controller.step("toggle",objectID="Kitchen_CoffeeMachine_01")

    for task_single in data['tasks']:
        objectID = task_single['objectId']
        action = task_single['action']
        controller.step("tp", objectID=objectID)
        feed_back = controller.step(action, objectID=objectID)
        # breakpoint()
        
        for object in feed_back['sceneState']['objects']:
            if object['objectId'] == objectID:
                if action == f'toggle' and object['isToggled']:
                    success_rate += 1
                elif action == f'break' and object['isBroken']:
                    success_rate += 1
                elif action == f'pick' and object['isPickedUp']:
                    success_rate += 1
                elif action == f'open' and object['isOpen']:
                    success_rate += 1
                else:
                    print(f'Failure with action {action} and object {objectID}')
        controller.reset_environment()
        # controller.reset_scene(scene="kitchen",robottype='h1')
        # feed_back['sceneState']['objects'][0]['isToggled']
        # feed_back['sceneState']['objects'][0]['isOpen']
        # feed_back['sceneState']['objects'][0]['isPickedUp']
        # feed_back['sceneState']['objects'][0]['isBroken']
        print(f'processing sucess rate is {success_rate}')
    print(f'final sucess rate is {success_rate}')

   

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    test_controller()
