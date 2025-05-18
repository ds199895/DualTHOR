import base64
import json
import os
import re
import logging
from controller import Controller
import base64
from openai import OpenAI

def get_visible_objects_info(objects_info):
    visible_objects_list = []
    visible_objects_id = []
    for object_unit in objects_info:
        object_visible_state = object_unit['visible']
        if object_visible_state:
            visible_objects_list.append(object_unit['name'])
            visible_objects_id.append(object_unit['objectId'])
    return visible_objects_list, visible_objects_id

def encode_image(image_path):
    with open(image_path, "rb") as image_file:
        return base64.b64encode(image_file.read()).decode("utf-8")

def find_max_numbered_file(imagepath):
    pattern = re.compile(r'^FrontCam_(\d+)')
    
    max_number = -1
    max_file = None
    
    try:
        # iterate over files in directory, filter files that match the pattern
        for file in os.listdir(imagepath):
            match = pattern.match(file)
            if match:
                # extract number and convert to integer
                number = int(match.group(1))
                # update max value and corresponding file name
                if number > max_number:
                    max_number = number
                    max_file = file
    except FileNotFoundError:
        print(f"path {imagepath} does not exist")
        return None
    
    if max_file:
        return os.path.join(imagepath, max_file)
    else:
        print("no file found")
        return None
    

def stringtrasnfer(output):
    # initialize results
    action, obj = None, None

    # try to parse as JSON
    try:
        # remove extra whitespace characters
        output = output.strip()
        # try to parse as JSON
        data = json.loads(output)
        # extract action and object fields
        action = data.get("action")
        obj = data.get("object")
        return action, obj
    except json.JSONDecodeError:
        # if not JSON format, continue to try to extract using regex
        pass

    # use regex to extract action and object values
    action_pattern = re.compile(r'"action"\s*:\s*"([^"]+)"')
    object_pattern = re.compile(r'"object"\s*:\s*(null|"[^"]*")')

    # extract action
    action_match = action_pattern.search(output)
    if action_match:
        action = action_match.group(1)  # extract value in double quotes

    # extract object
    object_match = object_pattern.search(output)
    if object_match:
        obj = object_match.group(1)  # extract value (null or string)
        if obj == "null":  # handle null value
            obj = None
        elif obj.startswith('"') and obj.endswith('"'):  # remove double quotes from string
            obj = obj[1:-1]

    # return results
    return action, obj


client = OpenAI(api_key='Your API KEY')

success_rate = 0
test_num = 20
test_epoch = 50

controller = Controller(config_path="config.json", start_unity_exe=False,robot_type='h1', scene="kitchen")

controller.start()

interact_action_list = ['pick', 'place', 'toggle', 'open', 'break']
move_list = ['moveahead', 'moveright', 'moveback', 'moveleft']
rotate_list = ['rotateright', 'rotateleft']
navigation_action_list = move_list + rotate_list
action_list = interact_action_list + navigation_action_list

json_file = f'./task_list.json'
with open(json_file, 'r') as file:
    data = json.load(file)

for task_single in data['tasks']:
    objectID = task_single['objectId']
    action = task_single['action']
    instruction = f'{action} the {objectID}'
    # controller.step("tp", objectID=objectID)
    # feed_back = controller.step(action, objectID=objectID)
    # breakpoint()
    for j in range(test_num):
        sum_success_count = 0
        success_count = 0
        controller.reset_environment()
        i = 0
        while(i < test_epoch):
        # for i in range(test_epoch):
            if i == 0:
                # initial_state = controller.step("getcurstate")
                initial_state = controller.step("moveahead",magnitude=0.1)
                imagepath = initial_state['imgpath']
                objects_info = initial_state['sceneState']['objects']
                visible_objects_list, visible_objects_id = get_visible_objects_info(objects_info)

            prompt = f'''You are a humanoid robot h1 in a household environment. Your task is to {instruction} in the house. The input image is the robot's first-person perspective, from the camera above its head. You need to follow the chain of thought and give the actions required to complete the task.
            ### Analyze the environment feedback
                1. There are {len(objects_info)} objects in this house. The visible object lists from environment feedback are {visible_objects_list}.
                2. The action for robots are {action_list}. You should choose one <action-decision> from them to excute the robot.
                3. If the object we need to interact with is in the visible object lists, you should choose <action-decision> from {interact_action_list} to complete the task. If the object is not in the visible object lists, you should choose <action-decision> from {navigation_action_list} to make the task-related object visible in the next environment feedback.
                4. You need output the action you choose and give the reasoning.

            ### Analyze the input image
                1. As we get the visible object lists from the environment feedback, we should recognize where the object is.
                2. If the object we need to interact with is in the visible object lists, you should choose <objectid-decision> from {visible_objects_id} to let the robot interact with the right object. If the object is not in the visible object lists, you need to determine which direction to move to get closer to the object you want to interact with. At this time <objectid-decision> shoule be None. 
                3. You should rethink whether the <action-decision> obtained from the environment feedback in the previous step is reasonable or not based on the image content. If it is reasonable, keep the <action-decision> unchanged. If it needs to be optimized, give the optimized action.
                3. You need output the action you choose and give the reasoning.

            ### Output format 
                1. After analyzing the environment feedback and the input image, you need to make a summary and output in a formal format. The output should be in a json format:
            {{
                "action": <action-decision>
                "object": <objectid-decision>
                "reasoning": 1. xxxxxx
                                2. xxxxxx
                                3. xxxxxx
            }}'''

            
            image_path = find_max_numbered_file(imagepath)
            base64_image = encode_image(image_path)
            prompt = prompt
            try:
                response = client.responses.create(
                    model="gpt-4o",
                    input=[
                        {
                            "role": "user",
                            "content": [
                                { "type": "input_text", "text": f'{prompt}' },
                                {
                                    "type": "input_image",
                                    "image_url": f"data:image/png;base64,{base64_image}",
                                },
                            ],
                        }
                    ],
                )
            except Exception as e:
                continue
            action, object = stringtrasnfer(response.output_text)
            # breakpoint()
            if action in interact_action_list:
                # print(f'interaction action is: {action}')
                feedback = controller.step(action, objectID=object)
            elif action in move_list:
                # print(f'navigation action is {action}')
                feedback = controller.step(action, magnitude = 0.6)
            elif action in rotate_list:
                feedback = controller.step(action, magnitude = 1)
            else:
                # print(f'no interaction')
                i = i - 1
            
            print(f'Action excution for {instruction} in number{j} at {i} step.')
            # print(feedback)

            i = i + 1            
            imagepath = feedback['imgpath']
            objects_info = feedback['sceneState']['objects']
            visible_objects_list, visible_objects_id = get_visible_objects_info(objects_info)
            
        for object in feedback['sceneState']['objects']:
            if object['objectId'] == objectID:
                if action == f'toggle' and object['isToggled']:
                    success_count += 1
                    print(f'*'*8)
                    print(f'Test number is {j}')
                    print(f'Test instruction is {instruction}')
                    print(f'Test result is success.')
                elif action == f'break' and object['isBroken']:
                    success_count += 1
                    print(f'*'*8)
                    print(f'Test number is {j}')
                    print(f'Test instruction is {instruction}')
                    print(f'Test result is success.')
                elif action == f'pick' and object['isPickedUp']:
                    success_count += 1
                    print(f'*'*8)
                    print(f'Test number is {j}')
                    print(f'Test instruction is {instruction}')
                    print(f'Test result is success.')
                elif action == f'open' and object['isOpen']:
                    success_count += 1
                    print(f'*'*8)
                    print(f'Test number is {j}')
                    print(f'Test instruction is {instruction}')
                    print(f'Test result is success.')
                else:
                    print(f'*'*8)
                    print(f'Test number is {j}')
                    print(f'Test instruction is {instruction}')
                    print(f'Test result is failure.')
    
    success_rate = float(success_count)/float(test_num)
    print(f'Task——{instruction} success rate is {success_rate}')