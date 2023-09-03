import sys; sys.path.append("utils")
import time; start_time = time.time()

# Import config file
import config

# Import torch libraries
import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
from torch.optim import lr_scheduler
from torch.utils.data import Dataset, DataLoader
from torchvision import transforms, datasets, models
from torchsummary import summary

# Import custom dataset class
from dataset import SaliencyDataset, get_random_datasets

# Import testing function
from test import test_model

def get_time():
	return ("[" + str("{:.4f}".format(time.time() - start_time)) + "]: ")

# uses the model to predict the salniency of one image
def pred_image(imagePath, id, file_names, outputFileName, use_gpu):

    # Check device to run on
    if (use_gpu == 1 and torch.cuda.is_available()):
        device =  torch.device("cuda:0");
    elif (use_gpu == 1 and not torch.cuda.is_available()):
        return 1
    else:
        device = torch.device("cpu")

    print(get_time() + "Working on " + str(device))
    time.sleep(5)

    # Load the model from disk
    PATH = config.model_path
    model = torch.load(PATH, map_location=device)
    # print(get_time() + "Model has been loaded.")
    
    # Saves the output images in the same path as the input ones
    config.test_ipath = imagePath
    config.test_save_path = imagePath

    # Generate a dataset with new test samples
    saliency_test_set = SaliencyDataset([id],config.test_ipath ,config.test_opath, transform = config.trans)

    # generate the corresponding data loader
    sal_test_loader = DataLoader(saliency_test_set, batch_size=1, shuffle=False, num_workers=0)

    # Test the model
    test_model(model, device, sal_test_loader, file_names, outputFileName)

    # print(get_time() + "Testing has been done.")

    # return config.test_save_path
    return 0

# uses the model to predict the saliency of a directory of images
def pred_dir(imagesPath, num_files, file_names, outputFileName, use_gpu):

    # Check device o run on
    if (use_gpu == 1 and torch.cuda.is_available()):
        device =  torch.device("cuda:0");
    elif (use_gpu == 1 and not torch.cuda.is_available()):
        return 1
    else:
        device = torch.device("cpu")

    print(get_time() + "Working on " + str(device))
    time.sleep(5)

    # Load the model from disk
    PATH = config.model_path
    model = torch.load(PATH, map_location=device)		
    # print(get_time() + "Model has been loaded.")

    # Saves the output images in the same path as the input ones
    config.test_ipath = imagesPath
    config.test_save_path = imagesPath
    config.test_total = num_files

    # Generate a dataset with new test samples
    saliency_test_set = SaliencyDataset(range(0,config.test_total),config.test_ipath,config.test_opath, transform = config.trans)

    # generate the corresponding data loader
    sal_test_loader = DataLoader(saliency_test_set, batch_size=1, shuffle=False, num_workers=0)

    # Test the model
    test_model(model, device, sal_test_loader, file_names, outputFileName, multitest=True)

    # print(get_time() + "Testing has been done.")
    # return config.test_save_path
    return 0
