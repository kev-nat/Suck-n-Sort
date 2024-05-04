// robot geometry
var e = 115.0;            // end effector
var f = 457.3;            // base
var re = 232.0;
var rf = 112.0;
var btf = 400.0;

// trigonometric constants
var sqrt3 = Math.sqrt(3.0);
var pi = Math.PI;//3.141592653;     // PI
var sin120 = sqrt3/2.0;   
var cos120 = -0.5;        
var tan60 = sqrt3;
var sin30 = 0.5;
var tan30 = 1.0/sqrt3;

// forward kinematics: (theta1, theta2, theta3) -> (x0, y0, z0)
// returned {error code, theta1,theta2,theta3}
function delta_calcForward(theta1, theta2, theta3) {
  var x0=0.0;
  var y0=0.0;
  var z0=0.0;
  
  var t = (f-e)*tan30/2.0;
  var dtr = pi/180.0;

  theta1 *= dtr;
  theta2 *= dtr;
  theta3 *= dtr;

  var y1 = -(t + rf*Math.cos(theta1));
  var z1 = -rf*Math.sin(theta1);

  var y2 = (t + rf*Math.cos(theta2))*sin30;
  var x2 = y2*tan60;
  var z2 = -rf*Math.sin(theta2);

  var y3 = (t + rf*Math.cos(theta3))*sin30;
  var x3 = -y3*tan60;
  var z3 = -rf*Math.sin(theta3);

  var dnm = (y2-y1)*x3-(y3-y1)*x2;

  var w1 = y1*y1 + z1*z1;
  var w2 = x2*x2 + y2*y2 + z2*z2;
  var w3 = x3*x3 + y3*y3 + z3*z3;

  // x = (a1*z + b1)/dnm
  var a1 = (z2-z1)*(y3-y1)-(z3-z1)*(y2-y1);
  var b1 = -((w2-w1)*(y3-y1)-(w3-w1)*(y2-y1))/2.0;

  // y = (a2*z + b2)/dnm;
  var a2 = -(z2-z1)*x3+(z3-z1)*x2;
  var b2 = ((w2-w1)*x3 - (w3-w1)*x2)/2.0;

  // a*z^2 + b*z + c = 0
  var a = a1*a1 + a2*a2 + dnm*dnm;
  var b = 2.0*(a1*b1 + a2*(b2-y1*dnm) - z1*dnm*dnm);
  var c = (b2-y1*dnm)*(b2-y1*dnm) + b1*b1 + dnm*dnm*(z1*z1 - re*re);

  // discriminant
  var d = b*b - 4.0*a*c;
  if (d < 0.0) return Array(1,0,0,0); // non-existing povar. return error,x,y,z
  
  z0 = -0.5*(b+Math.sqrt(d))/a;
  x0 = (a1*z0 + b1)/dnm;
  y0 = (a2*z0 + b2)/dnm;

  return Array(0,x0,y0,z0);// return error,x,y,z
}

// inverse kinematics
// helper functions, calculates angle theta1 (for YZ-pane)
function delta_calcAngleYZ(x0, y0, z0) {
  var y1 = -0.5 * 0.57735 * f;  // f/2 * tan(30 deg)
      y0 -= 0.5 * 0.57735 * e;  // shift center to edge

  // z = a + b*y
  var a = (x0*x0 + y0*y0 + z0*z0 +rf*rf - re*re - y1*y1)/(2.0*z0);
  var b = (y1-y0)/z0;

  // discriminant
  var d = -(a+b*y1)*(a+b*y1)+rf*(b*b*rf+rf); 
  if (d < 0) return Array(1,0); // non-existing povar.  return error, theta

  var yj = (y1 - a*b - Math.sqrt(d))/(b*b + 1); // choosing outer povar
  var zj = a + b*yj;
  theta = Math.atan(-zj/(y1 - yj)) * 180.0/pi + ((yj>y1)?180.0:0.0);

  return Array(0,theta);  // return error, theta
}

// inverse kinematics: (x0, y0, z0) -> (theta1, theta2, theta3)
// returned {error code, theta1,theta2,theta3}
function delta_calcInverse(x0, y0, z0) {
  var theta1 = 0;
  var theta2 = 0;
  var theta3 = 0;
  var status = delta_calcAngleYZ(x0, y0, z0);
  
  if(status[0] == 0) {
    theta1=status[1];
    status = delta_calcAngleYZ(x0*cos120 + y0*sin120, y0*cos120-x0*sin120, z0, theta2);  // rotate coords to +120 deg
  }
  if(status[0] == 0) {
    theta2=status[1];
    status = delta_calcAngleYZ(x0*cos120 - y0*sin120, y0*cos120+x0*sin120, z0, theta3);  // rotate coords to -120 deg
  }
  
  theta3=status[1];

  return Array( status[0], theta1,theta2,theta3 );
}

function roundoff(x,y) {
  z=Math.pow(10,y);
  if(y==undefined) y=3;
  return Math.round(x*z)/z;
}

function read_inputs() {
  e   = parseFloat($('#e').val());
  f   = parseFloat($('#f').val());
  re  = parseFloat($('#re').val());
  rf  = parseFloat($('#rf').val());
  s   = parseInt($('#s').val());
  btf = parseFloat($('#b').val());
}

function test_bounds() {
  read_inputs();

  var maxx=-e-f-re-rf;
  var maxy=maxx;
  var maxz=maxx;
  var minx=-maxx;
  var miny=-maxx;
  var minz=-maxx;
  var sd=360.0/s;
  var x,y,z;
  
  // find extents
  for(z=0;z<s;++z) {
    r=delta_calcForward(z*sd,z*sd,z*sd);
    if(r[0]==0) {
      if(minz>r[3]) minz=r[3];
      if(maxz<r[3]) maxz=r[3];
    }
  }
  if(minz<-btf) minz=-btf;
  if(maxz<-btf) maxz=-btf;

  var middlez=(maxz+minz)*0.5;

//  $('#output').append("<p>("+maxz+","+minz+","+middlez+")</p>");
  var original_dist=(maxz-middlez);
  var dist=original_dist*0.5;
  var sum=0;
  var r=Array(8);
  var mint1= 360;
  var maxt1=-360;
  var mint2= 360;
  var maxt2=-360;
  var mint3= 360;
  var maxt3=-360;
  
  do {
    sum+=dist;
    r[0]=delta_calcInverse(+sum,+sum,middlez+sum);
    r[1]=delta_calcInverse(+sum,-sum,middlez+sum);
    r[2]=delta_calcInverse(-sum,-sum,middlez+sum);
    r[3]=delta_calcInverse(-sum,+sum,middlez+sum);
    r[4]=delta_calcInverse(+sum,+sum,middlez-sum);
    r[5]=delta_calcInverse(+sum,-sum,middlez-sum);
    r[6]=delta_calcInverse(-sum,-sum,middlez-sum);
    r[7]=delta_calcInverse(-sum,+sum,middlez-sum);

    if(r[0][0]!=0 || r[1][0]!=0 || r[2][0]!=0 || r[3][0]!=0 || 
       r[4][0]!=0 || r[5][0]!=0 || r[6][0]!=0 || r[7][0]!=0 ) {
      sum-=dist;
      dist*=0.5;
    } else {
      for(i=0;i<8;++i) {
        if(mint1>r[i][1]) mint1=r[i][1];
        if(maxt1<r[i][1]) maxt1=r[i][1];
        if(mint2>r[i][2]) mint2=r[i][2];
        if(maxt2<r[i][2]) maxt2=r[i][2];
        if(mint3>r[i][3]) mint3=r[i][3];
        if(maxt3<r[i][3]) maxt3=r[i][3];
      }
    }
  } while( original_dist>sum && dist > 0.1 );
 
  var home = delta_calcForward(0,0,0);
  $('#center').html("(0,0,"+roundoff(middlez,3)+")");
  $('#home').html("(0,0,"+roundoff(home[3],3)+")");
  $('#bounds').html("X="+roundoff(-sum,3)+" to "+roundoff(sum,3)+" mm"
                   +"<br />Y="+roundoff(-sum,3)+" to "+roundoff(sum,3)+" mm"
                   +"<br />Z="+roundoff(middlez-sum,3)+" to "+roundoff(middlez+sum,3)+" mm");
  $('#limits').html("theta 1="+roundoff(mint1,2)+" to "+roundoff(maxt1,2)
                   +"<br />theta 2="+roundoff(mint2,2)+" to "+roundoff(maxt2,2)
                   +"<br />theta 3="+roundoff(mint3,2)+" to "+roundoff(maxt3,2));
  
  // resolution?  
  r1=delta_calcForward(0,0,0);
  r2=delta_calcForward(sd,0,0);
  
  x=(r1[1]-r2[1]);
  y=(r1[2]-r2[2]);
  sum=Math.sqrt(x*x+y*y);
  
  $('#res').html("+/-"+roundoff(sum,3)+"mm");
}

function test_fk_ik_match() {
  read_inputs();
  theta1=parseFloat($('#t1').val());
  theta2=parseFloat($('#t2').val());
  theta3=parseFloat($('#t3').val());
  results1=delta_calcForward(theta1,theta2,theta3);
  results2=delta_calcInverse(results1[1],results1[2],results1[3]);

  $('#x').val(roundoff(results1[1],3));
  $('#y').val(roundoff(results1[2],3));
  $('#z').val(roundoff(results1[3],3));
}

function test_fk() {
  read_inputs();
  theta1=parseFloat($('#t1').val());
  theta2=parseFloat($('#t2').val());
  theta3=parseFloat($('#t3').val());
  results1=delta_calcForward(theta1,theta2,theta3);
  $('#x').val(roundoff(results1[1],3));
  $('#y').val(roundoff(results1[2],3));
  $('#z').val(roundoff(results1[3],3));
}

function test_ik() {
  read_inputs();
  x=parseFloat($('#x').val());
  y=parseFloat($('#y').val());
  z=parseFloat($('#z').val());
  results2=delta_calcInverse(x,y,z);
  $('#t1').val(roundoff(results2[1],3));
  $('#t2').val(roundoff(results2[2],3));
  $('#t3').val(roundoff(results2[3],3));
}

function test() {
  test_bounds();
  test_fk_ik_match();
}

$(document).ready(function() {
  test();
});