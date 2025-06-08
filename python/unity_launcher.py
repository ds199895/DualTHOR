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

# volcengine TOS configuration
TOS_ACCESS_KEY = 'AKLTMWJkNmZiMmRmODhiNGNlNTk1Nzc3NDJmNTBiNThjNGM'
TOS_SECRET_KEY = 'Wm1JNE1HWmxNalZpTXpZd05ESmtOVGxsWm1ZellqZGxaV1JrWTJJd1lUTQ=='
TOS_ENDPOINT = 'tos-cn-beijing.volces.com'  # for example: 'tos-cn-beijing.volces.com'
TOS_REGION='cn-beijing'
TOS_BUCKET_NAME = 'unity-agent-playground'

# select Unity executable file path based on operating system
if platform.system() == "Windows":
    UNITY_EXECUTABLE_PATH = Path(__file__).parent.parent / "unity" / "Build" / "Win" / "Playground.exe"
elif platform.system() == "Darwin":  # macOS
    UNITY_EXECUTABLE_PATH = Path(__file__).parent.parent / "unity" / "Build" / "Mac" / "Playground.app"
elif platform.system() == "Linux":
    UNITY_EXECUTABLE_PATH = Path(__file__).parent.parent / "unity" / "Build" / "Linux" / "Playground.x86_64"
else:
    raise Exception("Unsupported operating system")

# select Unity executable file path based on operating system
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
    class for displaying download progress
    """
    def __init__(self, total_size, desc):
        self.pbar = tqdm(total=total_size, unit='B', unit_scale=True, desc=desc)
        self.last_consumed = 0
    
    def __call__(self, consumed_bytes, total_bytes, rw_once_bytes, type: DataTransferType):
        """
        callback function for TOS download progress
        """
        increment = consumed_bytes - self.last_consumed
        self.last_consumed = consumed_bytes
        self.pbar.update(increment)
    
    def close(self):
        self.pbar.close()

def get_local_version(local_dir):
    """
    get local version, return None if not exist
    """
    version_file = os.path.join(local_dir, "version.json")
    try:
        if os.path.exists(version_file):
            with open(version_file, 'r') as f:
                version_data = json.load(f)
                return version_data.get('version')
    except Exception as e:
        print(f"failed to read local version info: {e}")
    return None

def get_tos_version(client, tos_folder):
    """
    get TOS version, return None if not exist
    """
    try:
        version_path = f"{tos_folder}/version.json"
        response = client.get_object(TOS_BUCKET_NAME, version_path)
        version_content = response.read()
        version_data = json.loads(version_content)
        return version_data.get('version')
    except Exception as e:
        print(f"failed to get TOS version info: {e}")
        return None

def percentage(consumed_bytes, total_bytes, rw_once_bytes, type: DataTransferType):
    if total_bytes:
        rate = int(100 * float(consumed_bytes) / float(total_bytes))
        print("rate:{}, consumed_bytes:{}, total_bytes:{}, rw_once_bytes:{}, type:{}".format(
            rate, consumed_bytes, total_bytes, rw_once_bytes, type))

def download_folder_from_tos(local_dir, tos_folder):
    """
    download zip file from volcengine TOS and unzip to specified directory.
    check version first, only download when TOS version is newer than local version.
    
    Args:
        local_dir: unzip target directory
        tos_folder: folder of zip file in TOS
    """
    try:
        # create TosClientV2 object
        client = tos.TosClientV2(TOS_ACCESS_KEY, TOS_SECRET_KEY, TOS_ENDPOINT, TOS_REGION)
        
        # check version
        local_version = get_local_version(local_dir)
        tos_version = get_tos_version(client, tos_folder)
        
        if tos_version is None:
            print("cannot get TOS version, skip download")
            return
            
        if local_version is not None and local_version >= tos_version:
            print(f"local version({local_version}) is the latest, skip download")
            return
            
        print(f"new version({tos_version}) found, start update...")
        
        # build TOS path and local temporary path for zip file
        zip_name = f"{platform.system().lower()}_unity.zip"
        tos_zip_path = f"{tos_folder}/{zip_name}"
        temp_zip_path = os.path.join(os.path.dirname(local_dir), zip_name)
        
        # get file size
        object_meta = client.head_object(TOS_BUCKET_NAME, tos_zip_path)
        total_size = object_meta.content_length
        
        # download zip file (with progress bar)
        print(f"start download zip file: {tos_zip_path}")
        progress_callback = TosProgress(total_size, "download progress")
        
        try:
            client.download_file(
                TOS_BUCKET_NAME, 
                tos_zip_path, 
                temp_zip_path,
                part_size=1024 * 1024 * 20,  # chunk size
                task_num=3,  # thread number
                data_transfer_listener=progress_callback  # progress bar
            )
        finally:
            progress_callback.close()
        
        print(f"zip file download completed: {temp_zip_path}")
        
        # ensure target directory exists
        if not os.path.exists(local_dir):
            os.makedirs(local_dir)
            
        # unzip file (with progress bar)
        print(f"start unzip file to: {local_dir}")
        with zipfile.ZipFile(temp_zip_path, 'r') as zip_ref:
            file_list = zip_ref.namelist()
            with tqdm(total=len(file_list), desc="unzip progress") as pbar:
                for file in file_list:
                    zip_ref.extract(file, local_dir)
                    pbar.update(1)
        
        print("file unzip completed")
        
        # download and save new version.json
        version_path = f"{tos_folder}/version.json"
        local_version_file = os.path.join(local_dir, "version.json")
        client.download_file(
            TOS_BUCKET_NAME,
            version_path,
            local_version_file
        )
        print("version info updated")
        
        # delete temporary zip file
        os.remove(temp_zip_path)
        print("temporary zip file deleted")
        
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
    set appropriate execution permissions for Unity executable file based on operating system
    """
    try:
        if platform.system() in ["Darwin", "Linux"]:
            # set execution permissions for Unix-like system (chmod +x)
            current_permissions = os.stat(UNITY_EXECUTABLE_PATH)
            os.chmod(UNITY_EXECUTABLE_PATH, current_permissions.st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
            print(f"execution permissions set: {UNITY_EXECUTABLE_PATH}")
        return True
    except Exception as e:
        print(f"failed to set execution permissions: {e}")
        return False

def start_unity(wait_time=5):
    """
    start Unity executable file and wait for specified time to ensure startup completed.
    """
    try:
        # download file
        download_folder_from_tos(UNITY_LOCAL_FOLDER,UNITY_TOS_FOLDER)

        # set execution permissions before startup
        if not set_executable_permissions():
            raise Exception("failed to set execution permissions")
            
        unity_process = subprocess.Popen([str(UNITY_EXECUTABLE_PATH)])
        print("Unity environment started.")
        time.sleep(wait_time)
        return unity_process
    except Exception as e:
        print(f"Failed to start Unity environment: {e}")
        return None

def stop_unity(unity_process):
    """
    stop Unity environment.
    """
    if unity_process and unity_process.poll() is None:
        unity_process.terminate()
        print("Unity environment terminated.")
    else:
        print("Unity process is already terminated or does not exist.")