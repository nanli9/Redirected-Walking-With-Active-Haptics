
import matplotlib.pyplot as plt
import matplotlib.patches as patches
from matplotlib.ticker import (AutoMinorLocator, MultipleLocator)
import numpy as np
from math import pi, cos, sin
from matplotlib.widgets import Slider, Button

origin = np.array([0,0])
radius_index = 0
straight = 1000000
allowed_radius = [7,14,21]
allowed_radius = allowed_radius + [straight] + [-r for r in reversed(allowed_radius)]
path = 1.2
min_path = 1
max_path = 7
arc_start_angle = 0.0  # starting dest_angle of the arc in degrees
initial_distance = 0.2

def getProjectionPoint(radius, path):
    center = np.array([-(radius + initial_distance), 0])
    theta = (path / (radius + initial_distance))
    return  center - center[0] * np.array([cos(theta), sin(theta)]), center + radius * np.array([cos(theta), sin(theta)])

# Function to calculate bounds for a path
def get_bounds_for_path(path):
    min_x = max_x = origin[0]
    min_y = max_y = origin[1]
    for r in allowed_radius:
        point, surface = getProjectionPoint(r, path)
        min_x = min(min_x, point[0], surface[0])
        min_y = min(min_y, point[1], surface[1])
        max_x = max(max_x, point[0], surface[0]) 
        max_y = max(max_y, point[1], surface[1])
    max_x = max(abs(min_x),max_x)
    min_x = -max_x
    padding = (max_x - min_x) * 0.15
    min_x -= padding
    max_x += padding
    padding = (max_y - min_y) * 0.05
    min_y -= padding
    max_y += padding
    return min_x, min_y, max_x, max_y

# Create the plot
fig, ax = plt.subplots()
plt.subplots_adjust(bottom=0.35)

radius_slider_ax = plt.axes([0.2, 0.08, 0.65, 0.10], facecolor='lightgoldenrodyellow')
radius_slider = Slider(radius_slider_ax, 'Radius', 0, len(allowed_radius)-1, valinit=radius_index, valstep=1)
radius_slider.valtext.set_text(str(allowed_radius[radius_index]))
path_slider_ax = plt.axes([0.2, 0.14, 0.65, 0.15], facecolor='lightgoldenrodyellow')
path_slider = Slider(path_slider_ax, 'Path', min_path, max_path, valinit=path, valstep=0.2)

# Adding a button for saving the ax
save_button_ax = plt.axes([0.8, 0.01, 0.1, 0.04])
save_button = Button(save_button_ax, 'Save', color='lightgoldenrodyellow', hovercolor='0.975')

def save_current_ax(event):
    # Get the current radius value
    extent = ax.get_window_extent().transformed(fig.dpi_scale_trans.inverted())
    current_radius = allowed_radius[int(radius_slider.val)]
    if current_radius == straight:
        filename = "Straight.png"
    else:
        filename = f"Radius_{current_radius}.png"
    # Save the current view of the ax to a file
    fig.savefig("./PNG/"+filename, bbox_inches=extent.expanded(1.4, 1.2))

save_button.on_clicked(save_current_ax)

def update(val):
    ax.clear()
    radius = allowed_radius[int(radius_slider.val)]
    radius_slider.valtext.set_text('Straight' if radius == straight else str(radius))
    path = float(path_slider.val)
    dest, _ = getProjectionPoint(radius,path)
    arrow, _ = getProjectionPoint(radius,path+0.01)
    center = np.array([-(radius + initial_distance), 0])
    dest_angle = (path / (radius+initial_distance)) 
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

    min_x, min_y, max_x, max_y = get_bounds_for_path(path)
    ax.set_aspect('equal')
    if max_x - min_x > 1.0:
        ax.xaxis.set_major_locator(MultipleLocator(1.0))
        ax.yaxis.set_major_locator(MultipleLocator(1.0))
    else:
        ax.xaxis.set_major_locator(MultipleLocator(0.1))
        ax.yaxis.set_major_locator(MultipleLocator(0.1))
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