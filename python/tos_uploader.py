import os
import tos
from tqdm import tqdm

# 火山引擎TOS配置
TOS_ACCESS_KEY = 'AKLTMWJkNmZiMmRmODhiNGNlNTk1Nzc3NDJmNTBiNThjNGM'
TOS_SECRET_KEY = 'Wm1JNE1HWmxNalZpTXpZd05ESmtOVGxsWm1ZellqZGxaV1JrWTJJd1lUTQ=='
TOS_ENDPOINT = 'tos-cn-beijing.volces.com'
TOS_REGION = 'cn-beijing'
TOS_BUCKET_NAME = 'unity-agent-playground'

class TosProgress(object):
    """
    用于显示上传进度的类
    """
    def __init__(self, total_size, desc):
        self.pbar = tqdm(total=total_size, unit='B', unit_scale=True, desc=desc)
        self.last_consumed = 0
    
    def __call__(self, consumed_bytes, total_bytes, rw_once_bytes, data_type):
        """
        TOS的进度回调函数
        """
        increment = consumed_bytes - self.last_consumed
        self.last_consumed = consumed_bytes
        self.pbar.update(increment)
    
    def close(self):
        self.pbar.close()

def get_tos_client():
    """
    创建TOS客户端
    """
    return tos.TosClientV2(TOS_ACCESS_KEY, TOS_SECRET_KEY, TOS_ENDPOINT, TOS_REGION)

def upload_file_to_tos(local_file_path, tos_folder):
    """
    将本地文件上传到TOS的目标目录下（如果重名则覆盖）
    
    Args:
        local_file_path: 本地文件路径
        tos_folder: TOS中的目标文件夹
    """
    try:
        client = get_tos_client()
        
        # 获取文件大小
        total_size = os.path.getsize(local_file_path)
        
        # 构建TOS路径
        file_name = os.path.basename(local_file_path)
        tos_file_path = f"{tos_folder}/{file_name}"
        
        # 上传文件（带进度条）
        print(f"开始上传文件: {local_file_path} 到 {tos_file_path}")
        progress_callback = TosProgress(total_size, "上传进度")
        
        try:
            with open(local_file_path, 'rb') as f:
                client.put_object(
                    bucket=TOS_BUCKET_NAME,
                    key=tos_file_path,
                    content=f,
                    data_transfer_listener=progress_callback
                )
        finally:
            progress_callback.close()
        
        print(f"文件上传完成: {tos_file_path}")
        
    except Exception as e:
        print(f"上传文件失败: {e}")
        raise

if __name__ == '__main__':
    # 示例用法
    # local_file = "D:/Agi/Unity/agent-playground/unity/Build/Linux/linux_unity.zip"
    # local_file = "D:/Agi/Unity/agent-playground/unity/Build/Win/windows_unity.zip"
    local_file = 'D:/Agi/Unity/agent-playground/unity/Build/Win/version.json'
    target_folder = 'scenes/Win'
    # target_folder='scenes/Win'
    upload_file_to_tos(local_file, target_folder)