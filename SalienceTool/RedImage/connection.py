import socket
import struct
import traceback
import logging
import time
import numpy as np

import executeMain

def sending_and_reciveing():
    s = socket.socket()
    socket.setdefaulttimeout(None)
    # print('socket created ')
    port = 60000
    s.bind(('127.0.0.1', port)) #local host
    s.listen(1) # 1 maximun conexions
    # print('socket listensing ... ')
    try:
        c, _ = s.accept() #when port connected

        bytes_received = c.recv(4000) #received bytes
        array_received = np.frombuffer(bytes_received, dtype=np.float32) #converting into float array
        # print("Received raw: ", array_received)

        if array_received[0] == 1: # Single image prediction
            # The id is not transformed to string
            id = int(array_received[array_received.size - 1])

            # Transforms the float array to string
            array_received = ''.join(chr(int(value) + ord('A') - 1) for value in array_received)
            imagePath = array_received[1:len(array_received) - 1]
            # print("SINGLE: ", "PATH" , imagePath, "ID: ", id)
            path = executeMain.pred_image(imagePath, id)


        elif array_received[0] == 2: # Directory prediction

            # The number of files is not transformed to string
            num_files = int(array_received[array_received.size - 1])

            # Transforms the float array to string
            array_received = ''.join(chr(int(value) + ord('A') - 1) for value in array_received)
            imagePath = array_received[1:len(array_received) - 1]
            # print("DIR: ", "PATH" , imagePath, "NUM_FILES: ", num_files)
            
            path = executeMain.pred_dir(imagePath, num_files)

        nn_output = path;
        nn_output = [ord(char.upper()) - ord('A') + 1.0 for char in nn_output]

        bytes_to_send = struct.pack('%sf' % len(nn_output), *nn_output) #converting float to byte
        c.sendall(bytes_to_send) #sending back
        c.close()
    except Exception as e:
        logging.error(traceback.format_exc())
        # print("error")
        c.sendall(bytearray([]))
        c.close()

sending_and_reciveing() 