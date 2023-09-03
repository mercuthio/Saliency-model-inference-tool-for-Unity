import sys
import os
import shutil
import config
import os
import numpy as np
import torch
import config
from DataLoader360Video import RGB_and_OF, RGB
from torch.utils.data import DataLoader
import cv2
import tqdm
from utils import frames_extraction
from utils import save_video
import time
from inference import eval
import subprocess
from models import SST_Sal
import fnmatch

extensions = ["vp8", "mp4", "avi", "webm", "mov"]

def executeSalience(file_name, use_gpu):
    # Extract video frames if hasn't been done yet
    if not os.path.exists(os.path.join(config.videos_folder, 'frames')):
        frames_extraction(config.videos_folder)

    # Obtain video names from the new folder 'frames'
    inference_frames_folder = os.path.join(config.videos_folder, 'frames')
    video_test_names = os.listdir(inference_frames_folder)
    
    # Select the device
    if (use_gpu == 1 and torch.cuda.is_available()):
        device =  torch.device("cuda:0");
    elif (use_gpu == 1 and not torch.cuda.is_available()):
        return 2
    else:
        device = torch.device("cpu")
    
    print("The model will be running on", device, "device") 

    # Load the model
    model =  torch.load(config.inference_model, map_location=device)

    # Load the data. Use the appropiate data loader depending on the expected input data
    if config.of_available:
        test_video360_dataset = RGB_and_OF(inference_frames_folder, config.optical_flow_dir, None, video_test_names, config.sequence_length, split='test', load_names=True)
    else:
        test_video360_dataset = RGB(inference_frames_folder, None, video_test_names, config.sequence_length, split='test', load_names=True)

    # Merge all the sublists
    merged_sequences = []
    for sublist in test_video360_dataset.sequences:
        merged_sequences.extend(sublist)

    # Check if there has been an error
    for i in range(0, num_files):
        if i < 10:
            file_search = "00" + str(i) + "_????.png"
        else:
            file_search = "01" + str(i%10) + "_????.png"

        if not any(fnmatch.fnmatchcase(file, file_search) for file in merged_sequences):
            return 1

    test_data = DataLoader(test_video360_dataset, batch_size=config.batch_size, shuffle=False)

    eval(test_data, model, device, config.results_dir)

    i = 0
    # Save video with the results
    for video_name in video_test_names:
        save_video(os.path.join(inference_frames_folder, video_name), 
                os.path.join(config.results_dir, video_name),
                None,
                file_name[i] + "_predicted.avi")
        i += 1

    return 0

videoPath = sys.argv[1]
mode = sys.argv[2]
use_gpu = int(sys.argv[3])

num_files = 0
file_name = []

actual_path = os.path.dirname(__file__)
config.inference_model = os.path.join(actual_path, "models/SST_Sal_wo_OF.pth")

if mode == "1": # Single prediction
    folder_name = '\\'.join(videoPath.split('\\')[0:-1]) # Name of the folder
    file_name.append(videoPath.split('\\')[-1]) # Name of the video

    new_folder = folder_name + "\\temporal_video_folder"
    os.mkdir(new_folder)
    shutil.move(videoPath, new_folder + "\\" + file_name[0]) # Move the file to the new folder

    config.results_dir = folder_name 
    config.videos_folder = new_folder
    num_files = 1

else: # Folder prediction
    results_folder = videoPath + "\\results"
    try:
        os.mkdir(results_folder)
    except:
        pass
    config.results_dir = results_folder
    config.videos_folder = videoPath

    for i in os.listdir(videoPath + "\\"):
        if i.split('.')[-1] in extensions:
            num_files += 1
            file_name.append(i)

# Generates salience video
out = executeSalience(file_name, use_gpu)

# Deletes the 000 folders
try:
    for i in range(0, num_files):
        shutil.rmtree(config.results_dir + "\\" + str(i).zfill(3))
except:
    pass

if mode == "1":
    # Moves the video to its original folder and deletes the new folder
    shutil.move(new_folder + "\\" + file_name[0], videoPath) 
    shutil.rmtree(new_folder)
else:
    # Moves the videos from result to the correct folder
    for result in os.listdir(results_folder):
        shutil.move(results_folder + "\\" + result, videoPath) 
    shutil.rmtree(results_folder)
    # Deletes the frame folder
    shutil.rmtree(videoPath + "\\frames")

if out == 1:
    if mode == "1":
        open(config.results_dir + "\\Error.txt", "w")
    else:
        open(videoPath + "\\Error.txt", "w")
elif out == 2:
    open(config.results_dir + "\\Error_GPU.txt", "w")
