import socket
import logging

# 设置日志记录
logging.basicConfig(level=logging.INFO)

class TCPServer:
    def __init__(self, host='localhost', port=5678):
        self.host = host
        self.port = port
        self.server_socket = None
        self.conn = None
        self.addr = None

    def start(self):
        """
        启动 TCP 服务器，等待客户端连接。
        """
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.server_socket.bind((self.host, self.port))
        self.server_socket.listen()
        logging.info(f"Server listening on {self.host}:{self.port}")

        self.conn, self.addr = self.server_socket.accept()
        logging.info(f"Connected by {self.addr}")

    def send(self, data):
        """
        向客户端发送数据。
        """
        if not self.conn:
            raise Exception("No active connection to send data.")
        self.conn.sendall(data.encode())
        logging.info(f"Sent data: {data}")

    def receive(self):
        """
        从客户端接收数据。
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
        关闭 TCP 服务器。
        """
        if self.conn:
            self.conn.close()
            self.conn = None
        if self.server_socket:
            self.server_socket.close()
            self.server_socket = None
        logging.info("Server stopped.")