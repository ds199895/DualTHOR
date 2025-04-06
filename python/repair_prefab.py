import re
import os

def replace_monobehaviour_in_apple_prefabs(directory_path):
    # 基础路径
    base_path = directory_path
    
    # 获取Apple_11的内容作为模板
    apple11_path = os.path.join(base_path, 'Apple_11.prefab')
    with open(apple11_path, 'r', encoding='utf-8') as file:
        apple11_content = file.read()
    
    # 找到要替换成的新内容模板
    new_monobehaviour_pattern = r'--- !u!114 &\d+\s+MonoBehaviour:.*?m_Script: {fileID: 11500000, guid: 2e8ee1912a69c514cb7c4faa962e6e4f, type: 3}.*?parentReceptacleObjects: \[\]'
    new_monobehaviour_template = re.search(new_monobehaviour_pattern, apple11_content, re.DOTALL).group(0)
    
    # 获取所有苹果prefab文件
    apple_prefabs = [f for f in os.listdir(base_path) if f.startswith('Apple_') and f.endswith('.prefab') and f != 'Apple_11.prefab']
    
    success_count = 0
    fail_count = 0
    
    for prefab_file in apple_prefabs:
        try:
            prefab_path = os.path.join(base_path, prefab_file)
            
            # 读取prefab文件内容
            with open(prefab_path, 'r', encoding='utf-8') as file:
                prefab_content = file.read()
            
            # 提取prefab的ID（如Apple_13中的13）
            prefab_id = prefab_file.split('.')[0]  # 获取Apple_XX
            
            # 找到需要替换的MonoBehaviour部分
            old_monobehaviour_pattern = r'--- !u!114 &\d+\s+MonoBehaviour:.*?m_Script: {fileID: 11500000, guid: b439f6e4ef5714ee2a3643acf37b7a9d, type: 3}.*?CurrentlyContains: \[\]'
            old_monobehaviour_match = re.search(old_monobehaviour_pattern, prefab_content, re.DOTALL)
            
            if old_monobehaviour_match:
                old_monobehaviour = old_monobehaviour_match.group(0)
                
                # 提取原始GUID
                guid_pattern = r'--- !u!114 &(\d+)'
                guid_match = re.search(guid_pattern, old_monobehaviour)
                original_guid = guid_match.group(1) if guid_match else None
                
                if original_guid:
                    # 创建新的MonoBehaviour内容
                    new_monobehaviour = new_monobehaviour_template.replace('objectID: Apple_11', f'objectID: {prefab_id}')
                    new_monobehaviour = new_monobehaviour.replace('--- !u!114 &7408586714399972724', f'--- !u!114 &{original_guid}')
                    
                    # 替换内容
                    updated_content = prefab_content.replace(old_monobehaviour, new_monobehaviour)
                    
                    # 写入修改后的内容
                    with open(prefab_path, 'w', encoding='utf-8') as file:
                        file.write(updated_content)
                    
                    success_count += 1
                    print(f"成功更新 {prefab_file}")
                else:
                    print(f"无法提取GUID: {prefab_file}")
                    fail_count += 1
            else:
                print(f"无法找到需要替换的MonoBehaviour部分: {prefab_file}")
                fail_count += 1
        except Exception as e:
            print(f"处理 {prefab_file} 时出错: {str(e)}")
            fail_count += 1
    
    print(f"\n批量处理完成！成功: {success_count}, 失败: {fail_count}")

if __name__ == "__main__":
    # 指定苹果prefab文件所在的文件夹路径
    directory_path = 'E:/21-Unity/Projects/agent-playground/unity/Assets/Physics/SimObjsPhysics/Kitchen Objects/Apple/Prefabs'
    replace_monobehaviour_in_apple_prefabs(directory_path)
