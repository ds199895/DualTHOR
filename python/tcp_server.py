import socket
import logging
import json
import time
# set logging
logging.basicConfig(level=logging.INFO)

class TCPServer:
    def __init__(self, host='localhost', port=5678):
        self.host = host
        self.port = port
        self.server_socket = None
        self.conn = None
        self.addr = None
        self.on_connect = None  # add connection event callback

    def start(self):
        """
        start TCP server, waiting for client connection.
        """
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.server_socket.bind((self.host, self.port))
        self.server_socket.listen()
        logging.info(f"Server listening on {self.host}:{self.port}")

        self.conn, self.addr = self.server_socket.accept()
        
        logging.info(f"Connected by {self.addr}")
        
        # 触发连接事件回调
        if self.on_connect:
            self.on_connect()
        # while True:
        #     time.sleep(1)

    def send(self, data):
        """
        send data to client.
        """
        if not self.conn:
            raise Exception("No active connection to send data.")
        # 确保数据以换行符结尾，用于标识数据包结束
        if not data.endswith('\n'):
            data += '\n'
        self.conn.sendall(data.encode())
        logging.info(f"Sent data: {data}")

    def send_robot_type(self, robot_type):
        """
        send robot_type parameter to client, format as JSON.
        """
        if not self.conn:
            raise Exception("No active connection to send data.")
        
        # convert robot_type to JSON format
        data = json.dumps({"robot_type": robot_type})
        self.conn.sendall(data.encode() + b'\n')
        logging.info(f"Sent robot type as JSON: {data}")

    def receive(self):
        """
        receive data from client.
        """
        if not self.conn:
            raise Exception("No active connection to receive data.")
        
        data_buffer = b''
        while True:
            chunk = self.conn.recv(1024)
            if not chunk:
                raise Exception("Connection closed by client.")
            data_buffer += chunk
            if data_buffer.endswith(b'\n'):
                break
        received_data = data_buffer.decode()
        # logging.info(f"Received data: {received_data}")
        return received_data

    def stop(self):
        """
        close TCP server.
        """
        if self.conn:
            self.conn.close()
            self.conn = None
        if self.server_socket:
            self.server_socket.close()
            self.server_socket = None
        logging.info("Server stopped.")