
import matplotlib.pyplot as plt
import matplotlib.patches as patches
from matplotlib.ticker import (AutoMinorLocator, MultipleLocator)
import numpy as np
from math import pi, cos, sin
from matplotlib.widgets import Slider

origin = np.array([0,0])
radius_index = 0
straight = 1000000
allowed_radius = [5, 7, 10, 14, 19, 25]
allowed_radius = allowed_radius + [straight] + [-r for r in reversed(allowed_radius)]
path = 5
min_path = 2
max_path = 7
arc_start_angle = 0.0  # starting dest_angle of the arc in degrees
initial_distance = 0.2

def getProjectionPoint(radius, path):
    center = np.array([-(radius + initial_distance), 0])
    theta = (path / radius)
    return  center - center[0] * np.array([cos(theta), sin(theta)])

# Function to calculate bounds for a path
def get_bounds_for_path(path):
    min_x = max_x = origin[0]
    min_y = max_y = origin[1]
    for r in allowed_radius:
        point = getProjectionPoint(r, path)
        min_x = min(min_x, point[0])
        min_y = min(min_y, point[1])
        max_x = max(max_x, point[0]) 
        max_y = max(max_y, point[1])

    padding = (max_x - min_x) * 0.1
    min_x -= padding
    min_y -= padding
    max_x += padding
    max_y += padding
    min_x = min(min_x,1)
    min_y = min(min_y,1)
    return min_x, min_y, max_x, max_y

# Calculate bounds for each path on init
path_bounds = [get_bounds_for_path(p) for p in range(min_path, max_path+1)]





# Create the plot
fig, ax = plt.subplots()
plt.subplots_adjust(bottom=0.35)

radius_slider_ax = plt.axes([0.2, 0.08, 0.65, 0.10], facecolor='lightgoldenrodyellow')
radius_slider = Slider(radius_slider_ax, 'Radius', 0, len(allowed_radius)-1, valinit=radius_index, valstep=1)
radius_slider.valtext.set_text(str(allowed_radius[radius_index]))
path_slider_ax = plt.axes([0.2, 0.14, 0.65, 0.15], facecolor='lightgoldenrodyellow')
path_slider = Slider(path_slider_ax, 'Path', min_path, max_path, valinit=path, valstep=1)



def update(val):
    ax.clear()
    radius = allowed_radius[int(radius_slider.val)]
    radius_slider.valtext.set_text('Straight' if radius == straight else str(radius))
    path = int(path_slider.val)
    dest = getProjectionPoint(radius,path)
    arrow = getProjectionPoint(radius,path+0.01)
    center = np.array([-(radius + initial_distance), 0])
    dest_angle = (path / radius) 
    theta1 = arc_start_angle + (radius < 0) * dest_angle * 180 / pi
    theta2 = arc_start_angle + (radius >= 0) * dest_angle * 180 / pi

    ax.plot(*origin, 'ko')
    ax.plot(*dest, 'ro')
    ax.add_patch(patches.Circle(center, radius, color='saddlebrown', fill=False))
    ax.add_patch(patches.Arc(center, 2*(radius+initial_distance), 2*(radius+initial_distance), 
                    theta1=theta1, 
                    theta2=theta2,
                    edgecolor='royalblue'))
    ax.annotate('', xy=arrow, xytext=dest,
            arrowprops=dict(arrowstyle='-|>', color='royalblue', mutation_scale=10.0))

    min_x, min_y, max_x, max_y = path_bounds[path-min_path]
    ax.set_aspect('equal')
    ax.xaxis.set_major_locator(MultipleLocator(1))
    ax.yaxis.set_major_locator(MultipleLocator(1))
    ax.set_xlim(min_x, max_x)
    ax.set_ylim(min_y, max_y)
    ax.set_xlabel('X')
    ax.set_ylabel('Y')
    ax.set_title('Direction Vectors and Tangents')
    ax.set_axisbelow(True)
    ax.grid(True, linestyle='--', color='0.55')

radius_slider.on_changed(update)
path_slider.on_changed(update)
update(0)  # Initial update

plt.show()