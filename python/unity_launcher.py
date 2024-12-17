import platform
from pathlib import Path
import subprocess
import time
import os
import stat
import zipfile
import json
from tqdm import tqdm
import tos
from tos import DataTransferType

# 火山引擎TOS配置
TOS_ACCESS_KEY = 'AKLTMWJkNmZiMmRmODhiNGNlNTk1Nzc3NDJmNTBiNThjNGM'
TOS_SECRET_KEY = 'Wm1JNE1HWmxNalZpTXpZd05ESmtOVGxsWm1ZellqZGxaV1JrWTJJd1lUTQ=='
TOS_ENDPOINT = 'tos-cn-beijing.volces.com'  # 例如：'tos-cn-beijing.volces.com'
TOS_REGION='cn-beijing'
TOS_BUCKET_NAME = 'unity-agent-playground'

# 根据操作系统选择 Unity 可执行文件路径
if platform.system() == "Windows":
    UNITY_EXECUTABLE_PATH = Path(__file__).parent.parent / "unity" / "Build" / "Win" / "Playground.exe"
elif platform.system() == "Darwin":  # macOS
    UNITY_EXECUTABLE_PATH = Path(__file__).parent.parent / "unity" / "Build" / "Mac" / "Playground.app"
elif platform.system() == "Linux":
    UNITY_EXECUTABLE_PATH = Path(__file__).parent.parent / "unity" / "Build" / "Linux" / "Playground.x86_64"
else:
    raise Exception("Unsupported operating system")

# 根据操作系统选择 Unity 可执行文件路径
if platform.system() == "Windows":
    UNITY_TOS_FOLDER= "scenes/Win"
    UNITY_LOCAL_FOLDER=Path(__file__).parent.parent / "unity" / "Build" / "Win"
elif platform.system() == "Darwin":  # macOS
    UNITY_TOS_FOLDER = "scenes/Mac"
    UNITY_LOCAL_FOLDER=Path(__file__).parent.parent / "unity" / "Build" / "Mac"
elif platform.system() == "Linux":
    UNITY_TOS_FOLDER = "scenes/Linux"
    UNITY_LOCAL_FOLDER=Path(__file__).parent.parent / "unity" / "Build" / "Linux"
else:
    raise Exception("Unsupported operating system")

class TosProgress(object):
    """
    用于显示下载进度的类
    """
    def __init__(self, total_size, desc):
        self.pbar = tqdm(total=total_size, unit='B', unit_scale=True, desc=desc)
        self.last_consumed = 0
    
    def __call__(self, consumed_bytes, total_bytes, rw_once_bytes, type: DataTransferType):
        """
        TOS的进度回调函数
        """
        increment = consumed_bytes - self.last_consumed
        self.last_consumed = consumed_bytes
        self.pbar.update(increment)
    
    def close(self):
        self.pbar.close()

# def get_tos_client():
#     """
#     创建TOS客户端
#     """
#     credential = StaticCredential(TOS_ACCESS_KEY, TOS_SECRET_KEY)
#     return TosClientV2(TOS_ENDPOINT, credential)

def get_local_version(local_dir):
    """
    获取本地版本号，如果不存在则返回None
    """
    version_file = os.path.join(local_dir, "version.json")
    try:
        if os.path.exists(version_file):
            with open(version_file, 'r') as f:
                version_data = json.load(f)
                return version_data.get('version')
    except Exception as e:
        print(f"读取本地版本信息失败: {e}")
    return None

def get_tos_version(client, tos_folder):
    """
    获取TOS上的版本号，如果不存在则返回None
    """
    try:
        version_path = f"{tos_folder}/version.json"
        response = client.get_object(TOS_BUCKET_NAME, version_path)
        version_content = response.read()
        version_data = json.loads(version_content)
        return version_data.get('version')
    except Exception as e:
        print(f"获取TOS版本信息失败: {e}")
        return None

def percentage(consumed_bytes, total_bytes, rw_once_bytes, type: DataTransferType):
    if total_bytes:
        rate = int(100 * float(consumed_bytes) / float(total_bytes))
        print("rate:{}, consumed_bytes:{}, total_bytes:{}, rw_once_bytes:{}, type:{}".format(
            rate, consumed_bytes, total_bytes, rw_once_bytes, type))

def download_folder_from_tos(local_dir, tos_folder):
    """
    从火山引擎TOS下载zip文件并解压到指定目录。
    先检查版本，只有当TOS上的版本比本地新时才下载。
    
    Args:
        local_dir: 解压目标目录
        tos_folder: TOS中zip文件所在的文件夹
    """
    try:
        # 创建 TosClientV2 对象
        client = tos.TosClientV2(TOS_ACCESS_KEY, TOS_SECRET_KEY, TOS_ENDPOINT, TOS_REGION)
        
        # 检查版本
        local_version = get_local_version(local_dir)
        tos_version = get_tos_version(client, tos_folder)
        
        if tos_version is None:
            print("无法获取TOS版本信息，跳过下载")
            return
            
        if local_version is not None and local_version >= tos_version:
            print(f"本地版本({local_version})已是最新，无需更新")
            return
            
        print(f"发现新版本({tos_version})，开始更新...")
        
        # 构建zip文件的TOS路径和本地临时路径
        zip_name = f"{platform.system().lower()}_unity.zip"
        tos_zip_path = f"{tos_folder}/{zip_name}"
        temp_zip_path = os.path.join(os.path.dirname(local_dir), zip_name)
        
        # 获取文件大小
        object_meta = client.head_object(TOS_BUCKET_NAME, tos_zip_path)
        total_size = object_meta.content_length
        
        # 下载zip文件（带进度条）
        print(f"开始下载压缩包: {tos_zip_path}")
        progress_callback = TosProgress(total_size, "下载进度")
        
        try:
            client.download_file(
                TOS_BUCKET_NAME, 
                tos_zip_path, 
                temp_zip_path,
                part_size=1024 * 1024 * 20,  # 分片大小
                task_num=3,  # 线程数
                data_transfer_listener=progress_callback  # 进度条
            )
        finally:
            progress_callback.close()
        
        print(f"压缩包下载完成: {temp_zip_path}")
        
        # 确保目标目录存在
        if not os.path.exists(local_dir):
            os.makedirs(local_dir)
            
        # 解压文件（带进度条）
        print(f"开始解压文件到: {local_dir}")
        with zipfile.ZipFile(temp_zip_path, 'r') as zip_ref:
            file_list = zip_ref.namelist()
            with tqdm(total=len(file_list), desc="解压进度") as pbar:
                for file in file_list:
                    zip_ref.extract(file, local_dir)
                    pbar.update(1)
        
        print("文件解压完成")
        
        # 下载并保存新的version.json
        version_path = f"{tos_folder}/version.json"
        local_version_file = os.path.join(local_dir, "version.json")
        client.download_file(
            TOS_BUCKET_NAME,
            version_path,
            local_version_file
        )
        print("版本信息已更新")
        
        # 删除临时zip文件
        os.remove(temp_zip_path)
        print("临时压缩包已删除")
        
    except tos.exceptions.TosClientError as e:
        print('fail with client error, message:{}, cause: {}'.format(e.message, e.cause))
    except tos.exceptions.TosServerError as e:
        print('fail with server error, code: {}'.format(e.code))
        print('error with request id: {}'.format(e.request_id))
        print('error with message: {}'.format(e.message))
        print('error with http code: {}'.format(e.status_code))
        print('error with ec: {}'.format(e.ec))
        print('error with request url: {}'.format(e.request_url))
    except Exception as e:
        print('fail with unknown error: {}'.format(e))

def set_executable_permissions():
    """
    根据操作系统为 Unity 可执行文件设置适当的执行权限
    """
    try:
        if platform.system() in ["Darwin", "Linux"]:
            # 为 Unix-like 系统设置执行权限 (chmod +x)
            current_permissions = os.stat(UNITY_EXECUTABLE_PATH)
            os.chmod(UNITY_EXECUTABLE_PATH, current_permissions.st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
            print(f"已设置执行权限: {UNITY_EXECUTABLE_PATH}")
        return True
    except Exception as e:
        print(f"设置执行权限失败: {e}")
        return False

def start_unity(wait_time=5):
    """
    启动 Unity 可执行文件，并等待指定的时间确保启动完成。
    """
    try:
        # 下载文件
        download_folder_from_tos(UNITY_LOCAL_FOLDER,UNITY_TOS_FOLDER)

        # 在启动前设置权限
        if not set_executable_permissions():
            raise Exception("无法设置执行权限")
            
        unity_process = subprocess.Popen([str(UNITY_EXECUTABLE_PATH)])
        print("Unity environment started.")
        time.sleep(wait_time)
        return unity_process
    except Exception as e:
        print(f"Failed to start Unity environment: {e}")
        return None

def stop_unity(unity_process):
    """
    关闭 Unity 环境。
    """
    if unity_process and unity_process.poll() is None:
        unity_process.terminate()
        print("Unity environment terminated.")
    else:
        print("Unity process is already terminated or does not exist.")