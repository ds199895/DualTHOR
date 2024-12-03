import json


class Actions:
    def __init__(self):
        # 动作方法映射表
        self.actions = {
            "moveahead": self.dynamic_action,
            "moveright": self.dynamic_action,
            "moveback": self.dynamic_action,
            "moveleft": self.dynamic_action,
            "rotateright": self.dynamic_action,
            "rotateleft": self.dynamic_action,
            "pick": self.dynamic_action,
            "place": self.dynamic_action,
            "resetjoint": self.dynamic_action,
            "tp": self.dynamic_action,
            "toggle": self.dynamic_action,
            "open": self.dynamic_action,
            "undo": self.dynamic_action,
            "redo": self.dynamic_action,
            "loadstate": self.dynamic_action,
        }

    def execute_action(self, action_name, **kwargs):
        """
        动态执行动作并传递参数。
        """
        action_method = self.actions.get(action_name.lower())
        if action_method:
            try:
                return json.dumps(action_method(action_name, **kwargs))
            except Exception as e:
                return json.dumps({"error": f"Error executing action '{action_name}': {str(e)}"})
        else:
            return json.dumps({"error": f"Unknown action: {action_name}"})

    def parse_parameters(self, params, action_name=None):
        """
        解析用户输入的参数列表为字典。
        :param params: 参数列表（格式：key=value 或单个参数）
        :param action_name: 当前的动作名称，用于特殊处理
        :return: 参数字典
        """
        kwargs = {}
        param_mapping = {
            "magnitude": "Magnitude",
            "successrate": "successRate",
        }

        # 特殊处理：loadstate 动作且只有一个参数
        if action_name and action_name.lower() == "loadstate" and len(params) == 1 and "=" not in params[0]:
            kwargs["stateID"] = params[0]
            return kwargs

        if len(params) == 1 and "=" not in params[0]:
            # 通用情况：仅有一个参数（例如 objectID）
            kwargs["objectID"] = params[0]
        else:
            for param in params:
                if "=" in param:
                    key, value = param.split("=", 1)
                    key = param_mapping.get(key.lower(), key)
                    try:
                        # 自动转换数值类型
                        if value.isdigit():
                            value = int(value)
                        elif self.is_float(value):
                            value = float(value)
                    except ValueError:
                        # 保留字符串原值
                        pass
                    kwargs[key] = value
                else:
                    key = param_mapping.get(param.lower(), param)
                    kwargs[key] = True  # 默认布尔参数

        return kwargs
    
    @staticmethod
    def is_float(value):
        """
        检查字符串是否可以转换为浮点数。
        """
        try:
            float(value)
            return True
        except ValueError:
            return False
        
    def dynamic_action(self, action_name, **kwargs):
        """
        动态动作处理，适配任意参数。
        """
        # 确保 stateID 始终是字符串类型
        if "stateID" in kwargs:
            kwargs["stateID"] = str(kwargs["stateID"])
        
        # 检查参数类型是否被支持
        for key, value in kwargs.items():
            if not isinstance(value, (int, float, str, bool)):
                return {"error": f"Unsupported parameter type for {key}: {type(value).__name__}"}
        
        return {"action": action_name, **kwargs}
    
    # Unsupported parameter type: String