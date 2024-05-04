import numpy as np
import pymongo
import cv2
import time
import io
import serial.tools.list_ports
from dotenv import load_dotenv, find_dotenv
from PIL import Image
from bson.binary import Binary
from ultralytics import YOLO
from ultralytics.utils.plotting import Annotator

# Serial Communication to Arduino
ports = serial.tools.list_ports.comports()
serialInst = serial.Serial()

portsList = []

for onePort in ports:
    portsList.append(str(onePort))
    print(str(onePort))

val = input("Select Port: COM")

for x in range(0, len(portsList)):
    if portsList[x].startswith("COM" + str(val)):
        portVar = "COM" + str(val)
        print(portVar)

serialInst.baudrate = 9600
serialInst.port = portVar
serialInst.write_timeout = None
serialInst.open()

# MongoDB Connections
myclient = pymongo.MongoClient("ur MongoDB Atlas connection URL")
mydb = myclient["WasteSorting"]
mycol = mydb["Types of Trash"]
load_dotenv(find_dotenv())

collection = mydb.TypesofTrash

images = mydb.images
image = images.find_one()

# Insert item values and images to MongoDB 
def update_organic(name):
    collection.update_one(
        {"Name":name},
        {"$inc" : {"Amount": 1}}
        )
    collection.update_one(
        {"Name": "Organic"},
        {"$inc" : {"Amount": 1}}
        )
    collection.update_one(
        {'Name': 'Organic'}, 
        {'$set': {'Img': img_bson}})
                
def update_inorganic(name):
    collection.update_one(
        {"Name":name},
        {"$inc" : {"Amount": 1}}
        )
    collection.update_one(
        {"Name": "Inorganic"},
        {"$inc" : {"Amount": 1}}
        )
    collection.update_one(
        {'Name': 'Inorganic'}, 
        {'$set': {'Img': img_bson}})

# Initialize video capture
cap = cv2.VideoCapture(0)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)

model = YOLO('bestv7.pt')

# Delay after detection (in seconds)
delay_after_detection = 0.25
last_detection_time = 0

# Perspective transformation points
imgPts = np.float32([[334, 181], [974, 178], [6, 587], [1249, 576]]) # Top Left, Top Right, Bottom Right, Bottom Left
objPoints = np.float32([[0, 0], [600, 0], [0, 600], [600, 600]])
matrix = cv2.getPerspectiveTransform(imgPts, objPoints)

while True:
    _, frame = cap.read()

    truePerspective = cv2.warpPerspective(frame,matrix,(600,600))
    
    results = model(truePerspective, agnostic_nms=True, show=True,conf=0.778)[0]
    
    for r in results:
        
        annotator = Annotator(truePerspective)
        
        boxes = r.boxes.cpu().numpy()
        for box in boxes:
            
            # Get box coordinates in (top, left, bottom, right) format
            b = box.xyxy[0]  
            c = box.xywh[0]
            
            # JSON doesn't support NumPy
            x=c[0]
            y=c[1]
            
            # Convert the coordinates into python float values
            x = np.interp(x,[0,600],[-190,170])
            y = np.interp(y,[0,600],[-115,219])
            
            # Convert float to integer
            x_int = int(x)
            y_int = int(y) 
            
            coord = str(x) + ' , '+str(y)
            annotator.box_label(b, coord)
            
            img = annotator.result()
            cv2.imshow("Main Camera", img)
            
    # Move tensor to CPU and convert to NumPy        
    c = results.boxes.cls.cpu().numpy() 
    
    # Output Labels
    Output_labels = results.names 
   
    current_time = time.time()
    
    if current_time - last_detection_time > delay_after_detection:
        
        for i in range(len(c)):
            
            class_num = int(c[i])
            print(x_int, y_int, class_num)
            
            # Take screenshots of the objects
            screenshot = frame.copy()
            
            # Convert to PIL Image
            screenshot_pil = Image.fromarray(cv2.cvtColor(screenshot, cv2.COLOR_BGR2RGB))
                        
            # Convert image to BSON binary format
            img_bytes = io.BytesIO()
            screenshot_pil.save(img_bytes, format='JPEG')
            img_bson = Binary(img_bytes.getvalue())
                   
            # 3D Print
            if class_num == 0:
                update_inorganic("3D Print")
                
            # Apple
            elif class_num == 1:
                update_organic("Apple")
                
            # Cardboard Box
            elif class_num == 2:
                update_inorganic("Cardboard")
                
            # Metal
            elif class_num == 3:
                update_inorganic("Metal")
                
            # Orange
            elif class_num == 4:
                update_organic("Orange")
            
            # Pineapple
            elif class_num == 5:
                update_organic("Pineapple")
            
            # Plastic Ashtray
            elif class_num == 6:
                update_inorganic("Plastic Ashtray")
            
            # Plastic Blue Cube
            elif class_num == 7:
                update_inorganic("Blue Plastic Cube")

            # Plastic Cup
            elif class_num == 8:
                update_inorganic("Medium Plastic Cup")

            # Plastic Green Cube
            elif class_num == 9:
                update_inorganic("Green Plastic Cube")
            
            # Plastic Red Cube
            elif class_num == 10:
                update_inorganic("Red Plastic Cube")
                
            # Plastic Sauce Packet
            elif class_num == 11:
                update_inorganic("Plastic Sauce Packet")
                
            # Plastic Small Cup
            elif class_num == 12:
                update_inorganic("Small Plastic Cup")

            # Starfruit
            elif class_num == 13:
                update_organic("Starfruit")
            
            # Watermelon
            else:
                update_organic("Watermelon")

            last_detection_time = current_time
            
            # Convert the integers to strings, join them with a comma, and encode the result to bytes
            data = f"{x_int},{y_int},{class_num}".encode('utf-8')
                        
            # Send the data
            time.sleep(0.1)
            serialInst.write(data)
            
            # Remove leading/trailing whitespace
            print(str(serialInst).strip())  
            time.sleep(4)   
            
            if cv2.waitKey(30) == 27:
                break

cap.release()
cv2.destroyAllWindows()