from torchvision import transforms
 
# Transforations to input images
trans = transforms.Compose([
	transforms.ToTensor()
])

# Total of training images
total = 3

# Number of images for training
train = 2

# Number of images for validating
# val = 80
val = 1

# Batch size for training
batch_size = 3

# Number of epochs to train
epochs = 1

# Path to input images
ipath = "data/augmented_inputs/"

# Path to output images
opath = "data/augmented_outputs/"

# Path to save model
model_path = "models/def_model_20200207_short.pth"

# Path to save checkpoints
ckpt_path = "checkpoints/model_name.tar"

# Test parameters

# Number of test images (in case you want to test multiple images)
test_total = 1

# Path to test inputs
test_ipath = ""

# Path to test GT outputs (to compare)
# Set it as None if there is no GT
test_opath = None

# Path to save test results
test_save_path = ""
