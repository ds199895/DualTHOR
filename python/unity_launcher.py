import subprocess
import time

UNITY_EXECUTABLE_PATH = r'D:\Program Files\Unity\agent-playground\Build\Win\Playground.exe'

def start_unity(wait_time=5):
    """
    启动 Unity 可执行文件，并等待指定的时间确保启动完成。
    
    参数:
    - wait_time (int): 等待 Unity 完全启动的时间（秒）。
    
    返回:
    - unity_process (Popen): 返回启动的 Unity 进程对象，如果失败则返回 None。
    """
    try:
        unity_process = subprocess.Popen([UNITY_EXECUTABLE_PATH])
        print("Unity environment started.")
        time.sleep(wait_time)  # 默认等待 5 秒或使用传入的等待时间
        return unity_process
    except Exception as e:
        print(f"Failed to start Unity environment: {e}")
        return None

def stop_unity(unity_process):
    """
    关闭 Unity 环境。
    
    参数:
    - unity_process (Popen): Unity 进程对象。
    """
    if unity_process and unity_process.poll() is None:  # 检查进程是否仍在运行
        unity_process.terminate()
        print("Unity environment terminated.")
    else:
        print("Unity process is already terminated or does not exist.")