import json
import logging

class ConfigManager:
    def __init__(self, file_path="config.json"):
        """
        初始化 ConfigManager，并加载配置文件。
        """
        self.file_path = file_path
        self.config = self.load_config()

    def load_config(self):
        """
        加载 config.json 文件，并返回其内容。
        """
        try:
            # logging.info(f"Loading config from: {self.file_path}")
            with open(self.file_path, 'r') as file:
                config = json.load(file)
                logging.info(f"Config loaded successfully.")
                return config
        except Exception as e:
            logging.error(f"Error loading config: {e}")
            return {}

    def get_success_rate(self, action_name):
        success_rates = self.config.get("success_rate", {})
        rate = success_rates.get(action_name.lower(), 1.0)
        # logging.debug(f"Fetching success rate for action '{action_name.lower()}': {rate}")
        return rate