import sys
import executeMain
import time
import os
import config

op = sys.argv[1]
path = sys.argv[2]
param = int(sys.argv[3])
outputFileName = sys.argv[4]
use_gpu = int(sys.argv[5])

actual_path = os.path.dirname(__file__)
config.model_path = os.path.join(actual_path, "models/def_model_20200207_short.pth")

file_names = []

for file in os.listdir(path):
    parts = file.split(".")
    extension = file.split(".")[len(parts)-1]
    if extension == "jpg" or extension == "png":
        file_names.append(file)


if op == "1":
    out = executeMain.pred_image(path, param, file_names[int(param)], outputFileName, use_gpu)
else:
    out = executeMain.pred_dir(path, param, file_names, outputFileName, use_gpu)

if out == 1:
    open(path + "\\Error_GPU.txt", "w")
