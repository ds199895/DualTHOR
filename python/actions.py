import json

class Actions:
    def __init__(self):
        # 定义所有的动作方法在这里
        self.actions = {
            "moveahead": self.move_ahead,
            "moveright": self.move_right,
            "moveback": self.move_back,
            "moveleft": self.move_left,
            "rotateright": self.rotate_right,
            "rotateleft": self.rotate_left,
            "pick": self.pick,
            "place": self.place,
            "resetjoint": self.reset_joint,  
            "tp": self.tp,  
            "toggle": self.toggle, 
            "open": self.open,
            "undo": self.undo,
            "redo": self.redo,
            "loadstate": self.loadstate 
        }

    def execute_action(self, action_name, **kwargs):
        # 查找并执行相应的动作方法
        action_method = self.actions.get(action_name.lower())
        if action_method:
            return json.dumps(action_method(**kwargs))
        else:
            return json.dumps({"error": f"Unknown action: {action_name}"})
        
    def loadstate(self, state_id):
        if not state_id:
            return {"error": "StateID is required for LoadSceneState."}
        return {"action": "LoadSceneState", "stateID": state_id}
    
    # 具体的动作方法（接收 move_magnitude 和 success_rate 参数）
    def move_ahead(self, move_magnitude=1.0, success_rate=1.0, **kwargs):
        return {"action": "MoveAhead", "moveMagnitude": move_magnitude, "successRate": success_rate}

    def move_right(self, move_magnitude=1.0, success_rate=1.0, **kwargs):
        return {"action": "MoveRight", "moveMagnitude": move_magnitude, "successRate": success_rate}

    def move_back(self, move_magnitude=1.0, success_rate=1.0, **kwargs):
        return {"action": "MoveBack", "moveMagnitude": move_magnitude, "successRate": success_rate}

    def move_left(self, moveMagnitude=1.0, successRate=1.0, **kwargs):
        return {"action": "MoveLeft", "moveMagnitude": moveMagnitude, "successRate": successRate}


    def rotate_right(self, move_magnitude=1.0, success_rate=1.0):
        return {"action": "RotateRight", "moveMagnitude": move_magnitude, "successRate": success_rate}

    def rotate_left(self, move_magnitude=1.0, success_rate=1.0):
        return {"action": "RotateLeft", "moveMagnitude": move_magnitude, "successRate": success_rate}

    def pick(self, move_magnitude=1.0, success_rate=1.0):
        parts = self.current_input.split()
        arm = "left" if "left" in self.current_input.lower() else "right"
        object_id = parts[2] if len(parts) > 2 else ""  
        return {"action": "Pick", "arm": arm, "objectID": object_id, "successRate": success_rate}

    def place(self, move_magnitude=1.0, success_rate=1.0):
        parts = self.current_input.split()
        arm = "left" if "left" in self.current_input.lower() else "right"
        object_id = parts[2] if len(parts) > 2 else ""  
        return {"action": "Place", "arm": arm, "objectID": object_id, "successRate": success_rate}
        
    def reset_joint(self, move_magnitude=1.0, success_rate=1.0):
        arm = "left" if "left" in self.current_input.lower() else "right"
        return {"action": "ResetJoint", "arm": arm, "successRate": success_rate}
    
    def tp(self, move_magnitude=1.0, success_rate=1.0):
        try:
            object_id = self.current_input.split(" ")[1]  
            return {"action": "TP", "objectID": object_id, "successRate": success_rate}
        except IndexError:
            return {"error": "Invalid TP command. Missing objectID.", "successRate": success_rate}
    
    def toggle(self, move_magnitude=1.0, success_rate=1.0):
        # 提取 objectID
        parts = self.current_input.split()
        object_id = parts[1] if len(parts) > 1 else ""  
        return {"action": "Toggle", "objectID": object_id, "successRate": success_rate}

    def open(self, move_magnitude=1.0, success_rate=1.0):
        # 提取 objectID
        parts = self.current_input.split()
        object_id = parts[1] if len(parts) > 1 else ""  
        return {"action": "Open", "objectID": object_id, "successRate": success_rate}

    def undo(self, move_magnitude=1.0, success_rate=1.0):
        return {"action": "Undo", "successRate": success_rate}

    def redo(self, move_magnitude=1.0, success_rate=1.0):
        return {"action": "Redo", "successRate": success_rate}

    # 新增属性保存当前输入，便于 pick 和 place 使用
    def set_current_input(self, user_input):
        self.current_input = user_input