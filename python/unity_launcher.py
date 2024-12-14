import platform
from pathlib import Path
import subprocess
import time
import os
import stat
import oss2
import zipfile
import json
from tqdm import tqdm

# 阿里云OSS配置
OSS_ACCESS_KEY_ID = 'LTAI5tARvTM8nnzURdDCx1DS'
OSS_ACCESS_KEY_SECRET = 'l2RwAZ8xHj1OkH4KYT1lPYN1ojyJ0n'
OSS_ENDPOINT = 'http://oss-cn-beijing.aliyuncs.com'
OSS_BUCKET_NAME = 'agent-playground'
OSS_OBJECT_NAME = 'your-object-name'

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
    UNITY_OSS_FOLDER= "scenes/Win"
    UNITY_LOCAL_FOLDER=Path(__file__).parent.parent / "unity" / "Build" / "Win"
elif platform.system() == "Darwin":  # macOS
    UNITY_OSS_FOLDER = "scenes/Mac"
    UNITY_LOCAL_FOLDER=Path(__file__).parent.parent / "unity" / "Build" / "Mac"
elif platform.system() == "Linux":
    UNITY_OSS_FOLDER = "scenes/Linux"
    UNITY_LOCAL_FOLDER=Path(__file__).parent.parent / "unity" / "Build" / "Linux"
else:
    raise Exception("Unsupported operating system")

class OssProgress(object):
    """
    用于显示下载进度的类
    """
    def __init__(self, total_size, desc):
        self.pbar = tqdm(total=total_size, unit='B', unit_scale=True, desc=desc)
    
    def update(self, consumed_bytes, total_bytes, *args):
        """
        OSS的进度回调函数，需要接收三个参数：
        consumed_bytes: 已经下载的字节数
        total_bytes: 总字节数
        args: 其他参数
        """
        # 计算这次更新的增量
        if not hasattr(self, 'last_consumed'):
            self.last_consumed = 0
        increment = consumed_bytes - self.last_consumed
        self.last_consumed = consumed_bytes
        
        # 更新进度条
        self.pbar.update(increment)
    
    def close(self):
        self.pbar.close()

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

def get_oss_version(bucket, oss_folder):
    """
    获取OSS上的版本号，如果不存在则返回None
    """
    try:
        version_path = f"{oss_folder}/version.json"
        version_content = bucket.get_object(version_path).read()
        version_data = json.loads(version_content)
        return version_data.get('version')
    except oss2.exceptions.NoSuchKey:
        print("OSS上不存在version.json文件")
        return None
    except Exception as e:
        print(f"获取OSS版本信息失败: {e}")
        return None

def download_folder_from_oss(local_dir, oss_folder):
    """
    从阿里云OSS下载zip文件并解压到指定目录。
    先检查版本，只有当OSS上的版本比本地新时才下载。
    
    Args:
        local_dir: 解压目标目录
        oss_folder: OSS中zip文件所在的文件夹
    """
    try:
        auth = oss2.Auth(OSS_ACCESS_KEY_ID, OSS_ACCESS_KEY_SECRET)
        bucket = oss2.Bucket(auth, OSS_ENDPOINT, OSS_BUCKET_NAME)
        
        # 检查版本
        local_version = get_local_version(local_dir)
        oss_version = get_oss_version(bucket, oss_folder)
        
        if oss_version is None:
            print("无法获取OSS版本信息，跳过下载")
            return
            
        if local_version is not None and local_version >= oss_version:
            print(f"本地版本({local_version})已是最新，无需更新")
            return
            
        print(f"发现新版本({oss_version})，开始更新...")
        
        # 构建zip文件的OSS路径和本地临时路径
        zip_name = f"{platform.system().lower()}_unity.zip"
        oss_zip_path = f"{oss_folder}/{zip_name}"
        temp_zip_path = os.path.join(os.path.dirname(local_dir), zip_name)
        
        # 获取文件大小
        object_meta = bucket.head_object(oss_zip_path)
        total_size = object_meta.content_length
        
        # 下载zip文件（带进度条）
        print(f"开始下载压缩包: {oss_zip_path}")
        progress_callback = OssProgress(total_size, "下载进度")
        
        try:
            bucket.get_object_to_file(
                oss_zip_path, 
                temp_zip_path,
                progress_callback=progress_callback.update
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
            # 获取压缩包中的所有文件
            file_list = zip_ref.namelist()
            # 创建解压进度条
            with tqdm(total=len(file_list), desc="解压进度") as pbar:
                for file in file_list:
                    zip_ref.extract(file, local_dir)
                    pbar.update(1)
        
        print("文件解压完成")
        
        # 下载并保存新的version.json
        version_path = f"{oss_folder}/version.json"
        local_version_file = os.path.join(local_dir, "version.json")
        bucket.get_object_to_file(version_path, local_version_file)
        print("版本信息已更新")
        
        # 删除临时zip文件
        os.remove(temp_zip_path)
        print("临时压缩包已删除")
        
    except Exception as e:
        print(f"下载或解压失败: {e}")
        raise

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
        download_folder_from_oss(UNITY_LOCAL_FOLDER,UNITY_OSS_FOLDER)

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