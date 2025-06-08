import json


class Actions:
    def __init__(self):
        # action method mapping table
        self.actions = {
            "moveahead": self.dynamic_action,
            "moveright": self.dynamic_action,
            "moveback": self.dynamic_action,
            "moveleft": self.dynamic_action,
            "moveup": self.dynamic_action,
            "movedown": self.dynamic_action,
            "rotateright": self.dynamic_action,
            "rotateleft": self.dynamic_action,
            "pick": self.dynamic_action,
            "place": self.dynamic_action,
            "resetjoint": self.dynamic_action,
            "tp": self.dynamic_action,
            "toggle": self.dynamic_action,
            "open": self.dynamic_action,
            "lift": self.dynamic_action,
            "undo": self.dynamic_action,
            "redo": self.dynamic_action,
            "loadstate": self.dynamic_action,
            "loadrobot": self.dynamic_action,
            "resetscene":self.dynamic_action,
            "getcurstate":self.dynamic_action,
            "resetpose":self.dynamic_action,
            "resetstate":self.dynamic_action
        }

    def execute_action(self, action_name, **kwargs):
        """
        dynamically execute action and pass parameters.
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
        parse user input parameter list to dictionary.
        :param params: parameter list (format: key=value or single parameter)
        :param action_name: current action name, for special processing
        :return: parameter dictionary
        """
        kwargs = {}
        param_mapping = {
            "magnitude": "Magnitude",
            "successrate": "successRate",
            "objectid": "objectID",
            "stateid": "stateID",
        }

        # special processing: loadstate action and only one parameter
        if action_name and action_name.lower() == "loadstate" and len(params) == 1 and "=" not in params[0]:
            kwargs["stateID"] = params[0]
            return kwargs

        if len(params) == 1 and "=" not in params[0]:
            # general case: only one parameter (e.g. objectID)
            kwargs["objectID"] = params[0]
        else:
            for param in params:
                if "=" in param:
                    key, value = param.split("=", 1)
                    key = param_mapping.get(key.lower(), key)
                    try:
                        # automatically convert numeric types
                        if value.isdigit():
                            value = int(value)
                        elif self.is_float(value):
                            value = float(value)
                    except ValueError:
                        # keep string original value
                        pass
                    kwargs[key] = value
                else:
                    key = param_mapping.get(param.lower(), param)
                    kwargs[key] = True  # default boolean parameter

        return kwargs
    
    @staticmethod
    def is_float(value):
        """
        check if string can be converted to float.
        """
        try:
            float(value)
            return True
        except ValueError:
            return False
        
    def dynamic_action(self, action_name, **kwargs):
        """
        dynamic action processing, adapt to any parameters.
        """
        # ensure stateID is always string type
        if "stateID" in kwargs:
            kwargs["stateID"] = str(kwargs["stateID"])
        
        # check if parameter type is supported
        for key, value in kwargs.items():
            if not isinstance(value, (int, float, str, bool)):
                return {"error": f"Unsupported parameter type for {key}: {type(value).__name__}"}
        
        return {"action": action_name, **kwargs}
    
    # Unsupported parameter type: String