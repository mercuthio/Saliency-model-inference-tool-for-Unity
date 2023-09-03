# Import torch utils
import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
from torch.optim import lr_scheduler
from torchvision import models
from torchsummary import summary

# Basic imports
import os
import sys
import cv2
import glob
import copy
import time
import numpy as np
import matplotlib.pyplot as plt
from collections import defaultdict

from scipy import ndimage

# Import config file
import config

# Test the model on an single image or on multiple images
def test_model(model, device, saliency_test_loader, file_names, outputFileName, multitest=False):
	
	# Test on a single image
	if not multitest:
		# Get sample (only 1)
		inputs, labels = next(iter(saliency_test_loader))
		
		# Prepare data
		inputs = inputs.to(device)

		# Predict
		model.eval()	
		pred = model(inputs)
		
		# Squeeze extra dims
		pred = np.squeeze(np.array(pred[0].detach().cpu()))

		# Clip image
		pred = np.clip(pred, 0, 1)

		if (outputFileName == ""):
			outputPath = config.test_save_path + file_names + "_predicted.png"
		else:
			outputPath = config.test_save_path + outputFileName + ".png"

		# Save it + Show if --plot flag
		plt.imshow(pred, cmap='gray')
		plt.axis('off')
		plt.savefig(outputPath, bbox_inches='tight', pad_inches=0)
		if "--plot" in sys.argv:
			plt.show()
		plt.clf()
		
		# if labels is not None:
		# 	# Save original
		# 	plt.imshow(np.squeeze(labels[0].cpu()), cmap='gray')
		# 	plt.axis('off')
		# 	plt.savefig(config.test_save_path + "gt.png", bbox_inches='tight')
		# 	if "--plot" in sys.argv:
		# 		plt.show()
		# 	plt.clf()
	
	# Multiple image prediction
	else:
	
		i = 0
		for inputs, labels in saliency_test_loader:
			# Prepare data
			inputs = inputs.to(device)

			# Predict
			model.eval()	
			pred = model(inputs)
			
			# Squeeze extra dims
			pred = np.squeeze(np.array(pred[0].detach().cpu()))
			
			# Clip
			pred = np.clip(pred, 0, 1)


			# Median filter
			pred = ndimage.median_filter(pred, size=9)
			
			# if i < 10:
			# 	name = "image_0" + str(i) + "_sphere.jpg"
			# else:
			# 	name = "image_" + str(i) + "_sphere.jpg"

			if (outputFileName == ""):
				name = file_names[i] + "_predicted.jpg"
			else:
				name = outputFileName + "_" + i + ".png"

			'''
			fig = plt.figure(frameon=False)
			plt.imshow(pred, cmap='gray')
			plt.axis('off')
			plt.savefig(config.test_save_path + name, bbox_inches='tight')
			plt.clf()
			'''

			##
			
			
			fig = plt.figure(frameon=False)
			fig.set_size_inches(2,1)
			ax = plt.Axes(fig, [0., 0., 1., 1.])
			ax.set_axis_off()
			fig.add_axes(ax)
			ax.imshow(pred, cmap='gray', aspect='auto')
			dpi = 256
			fig.savefig((config.test_save_path + name), dpi=dpi)
			
			print("Image " + str(i) + " completed.")
			i = i + 1
			

	