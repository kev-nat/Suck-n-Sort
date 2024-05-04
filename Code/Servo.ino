#include <Servo.h>
#include <math.h>

Servo myservo1;
Servo myservo2;
Servo myservo3;  

int pos1 = 75;
int pos2 = 75;
int pos3 = 75;

double e = 24.0; // End Effector radius
double f = 75.0; // Base radius
double re = 300.0; // Forearm length
double rf = 100.0; // Bicep length
double cos120 = -0.5;
double sin120 = sqrt(3) / 2;
double pi = 3.14159265358979323846; // PI

float x_cord = 0;
float y_cord = 0;
float class_num;

int relay_pin = 11;

void reset_zero(){
  myservo1.write(pos1); // +75 is straight down
  myservo2.write(pos2);
  myservo3.write(pos3);
  }

// Servo Calibration
void calibrate_servo1() {
  myservo1.write(150);
  delay(200);
  
  myservo1.write(75);
  delay(200);
  
  myservo1.write(32);
  delay(200);
  
  for (int angle = 32; angle <= 150; angle += 1) {
    myservo1.write(angle);
    delay(15);
  }
  
  for (int angle = 150; angle >= 32; angle -= 1) {
    myservo1.write(angle);
    delay(15);
  }
  delay(200);
}

void calibrate_servo2() {
  myservo2.write(150);
  delay(200);
  
  myservo2.write(75);
  delay(200);
  
  myservo2.write(32);
  delay(200);

  for (int angle = 32; angle <= 150; angle += 1) {
    myservo2.write(angle);
    delay(15);
  }
  
  for (int angle = 150; angle >= 32; angle -= 1) {
    myservo2.write(angle);
    delay(15);
  }
  delay(200);
}

void calibrate_servo3() {
  myservo3.write(150);
  delay(200);
  
  myservo3.write(75);
  delay(200);
  
  myservo3.write(55);
  delay(200);
  
  for (int angle = 55; angle <= 150; angle += 1) {
    myservo3.write(angle);
    delay(15);
  }
  
  for (int angle = 150; angle >= 55; angle -= 1) {
    myservo3.write(angle);
    delay(15);
  }
  delay(200);
}

// Inverse Kinematics Formula
int delta_calcAngleYZ(double x0, double y0, double z0, double &theta){
  double y1 = -0.5 * 0.57735 * f;  // f/2 * tg 30
  y0 -= 0.5 * 0.57735 * e;  // shift center to edge
  double a = (x0 * x0 + y0 * y0 + z0 * z0 + rf * rf - re * re - y1 * y1) / (2 * z0);
  double b = (y1 - y0) / z0;
  double d = -(a + b * y1) * (a + b * y1) + rf * (b * b * rf + rf);
  
  if (d < 0) {
    return -1;  // non-existing point
    }
    
    double yj = (y1 - a * b - sqrt(d)) / (b * b + 1);  // choosing outer point
    double zj = a + b * yj;
    theta = 180.0 * atan(-zj / (y1 - yj)) / pi + ((yj > y1) * 180.0);
    return 0;
}

void delta_calcInverse(double x0, double y0, double z0, double &halftheta1, double &halftheta2, double &halftheta3) {
  double theta1 = 0, theta2 = 0, theta3 = 0;
  int status = delta_calcAngleYZ(x0, y0, z0, theta1);
  
  if (status == 0) {
    status = delta_calcAngleYZ(x0 * cos120 + y0 * sin120, y0 * cos120 - x0 * sin120, z0, theta2);  // rotate coords to +120 deg
    }
    
  if (status == 0) {
    status = delta_calcAngleYZ(x0 * cos120 - y0 * sin120, y0 * cos120 + x0 * sin120, z0, theta3);  // rotate coords to -120 deg
    }
      
  if (status == 0) {
    halftheta1 = (theta1)+75;
    halftheta2 = (theta2)+75;
    halftheta3 = (theta3)+75;
  }
  
  // If the calculations failed, set the angles to 75 degrees
  else {
    halftheta1 = halftheta2 = halftheta3 = 75;
    }
    Serial.println(status);
    Serial.println(ceil(halftheta1));
    Serial.println(ceil(halftheta2));
    Serial.println(ceil(halftheta3));
    }

void relayON(){
  digitalWrite(relay_pin, LOW);
  }

void relayOFF(){
  digitalWrite(relay_pin, HIGH);
  }
        
void turn_right(){
  double right_1, right_2, right_3;
  delta_calcInverse(150, 0, -277.198, right_1, right_2, right_3);
  
  myservo1.write(right_1);
  myservo2.write(right_2);
  myservo3.write(right_3);
  delay(1000);
    
  relayOFF();
  delay(1000);
  
  reset_zero();
  delay(500);
  }

void turn_left(){
  double left_1, left_2, left_3;
  delta_calcInverse(-150, 0, -277.198, left_1, left_2, left_3);
  
  myservo1.write(left_1);
  myservo2.write(left_2);
  myservo3.write(left_3);
  delay(1000);
  
  relayOFF();
  delay(1000);
  
  reset_zero();
  delay(500);
  }

void down(){
  double down_1, down_2, down_3;
  delta_calcInverse(x_cord, y_cord, -226.362, down_1, down_2, down_3);
  
  myservo1.write(down_1);
  myservo2.write(down_2);
  myservo3.write(down_3);
  
  relayON();
  delay(2000);
}
    
void FindNSort(){
  
  // 3D Print
  if (class_num == 0) {
    down();
    turn_right();
    } 
    
    // Apple
    else if (class_num == 1) {
      down();
      turn_left();
      }
    
    // Cardboard
    else if (class_num == 2) {
      down();
      turn_right();
      }
        
    // Metal   
    else if (class_num == 3) {
      down();
      turn_right();
      }
          
    // Orange
    else if (class_num == 4) {
      down();
      turn_left();
      }
    
    // Pineapple
    else if (class_num == 5) {
      down();
      turn_left();
      }
              
    // Plastic Ashtray
    else if (class_num == 6) {
      down();
      turn_right();
      }
                
    // Blue Plastic Cube
    else if (class_num == 7) {
      down();
      turn_right();
      }
                  
    // Medium Plastic Cube
    else if (class_num == 8) {
      down();
      turn_right();
      }
                    
    // Green Plastic Cube
    else if (class_num == 9) {
      down();
      turn_right();
      }
                      
    // Red Plastic Cube
    else if (class_num == 10) {
      down();
      turn_right();
      }
                        
    // Plastic Sauce Packet
    else if (class_num == 11) {
      down();
      turn_right();
      }
                          
    // Small Plastic Cup
    else if (class_num == 12) {
      down();
      turn_right();
      }
                            
    // Starfruit
    else if (class_num == 13) {
      down();
      turn_left();
      }
    
    // Watermelon
    else if (class_num == 14) {
      down();
      turn_left();
      }
}

void setup() {
  Serial.begin(9600);
  myservo1.attach(10);  // attaches the servo on pin 10 to the servo object (green)
  myservo2.attach(9);   // attaches the servo on pin 9 to the servo object (yellow) 
  myservo3.attach(8);   // attaches the servo on pin 8 to the servo object (orange)
  pinMode(relay_pin, OUTPUT);
  
  relayOFF();
  
  reset_zero();
  delay(1000);
  
  calibrate_servo1();
  delay(1000);
  
  calibrate_servo2(); 
  delay(1000);
    
  calibrate_servo3();
  delay(1000);
  
  reset_zero();
}

void loop() {
  if (Serial.available() > 0) {
    x_cord = Serial.parseFloat();
    y_cord = Serial.parseFloat();
    class_num = Serial.parseFloat();
    
    double halftheta1, halftheta2, halftheta3;
    delta_calcInverse(x_cord, y_cord, -277.198, halftheta1, halftheta2, halftheta3);
    
    myservo1.write(halftheta1);
    myservo2.write(halftheta2);
    myservo3.write(halftheta3);
    
    FindNSort(); 
  }
}