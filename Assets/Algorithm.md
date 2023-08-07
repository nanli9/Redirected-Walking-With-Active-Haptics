# Want to Verify
* Impact of active repelling kinesthetic feedback on redirected walking(RDW).
* If my haptic device is improving RDW's physical spatial requirement.

# Conditions
1. Visual manipulation only (RDW)
2. Visual manipulation with kinesthetic force applied (RDW + stimuli)

# Initialization Algorithm
Only Called at the begining of the experiment

1. **Haptic Wall** $HW$, a cylindrical wall with **Radius** $r$ and **Height** $h$ on the **Left** $\hat{L}$ of the **User** $U$, where **Initial Comfortable Distance** $d$ between surface of the wall with the **User** is just enough to touch the wall with comfort. **Left** $\hat{L}$ and position of the **User** is measured.
$$HW =\langle U_x,h,U_z \rangle+(r+d)\cdot\hat{L} $$
2. Reset **Total Traveled Angle** $\theta$ to 0.
$$\theta = 0$$
3. Reset **Previous User Relative Angle** $\theta_p$ from difference between z component $U_z - HW_z$ and x component $U_x - HW_x$ of **User** and **Haptical Wall** in $-\pi$ to $\pi$ scale.
$$\theta_p = arctan2(U_z - HW_z,U_x - HW_x)$$

# Vision Rendering Algorithm
called every single frame
1. **Visual Wall** $VW$, infinite straight wall on the surface of the **Haptic Wall** and left of the **User**, and **Visual Floor** $VF$ where its transformation is calculated by:
    1. Get **Projected Vector** $\vec{V}$ from center of **Haptic Wall** $HW$ to position of **User** $U$
    $$\vec{V} = \langle \vec{U}_x - \vec{HW}_x,0,\vec{U}_z - \vec{HW}_z\rangle$$
    
    2. Get **Projected Point** $P$ by projecting **Projected Unit Vector** $\hat{V}$ from center of **Haptic Wall** $HW$ in **Radius** $r$ magnitude.
    $$P = \langle HW_x,0,HW_z \rangle+r\cdot\hat{V}$$ 
    
    3. Get clockwize **Tangent Unit Vector** $\hat{T}$ on the surface of **Haptic Wall** at **Projected Point** using absolute **Up** $\hat{y}$ direction, and **Projected Unit Vector** $\hat{V}$.
    $$\hat{T} = \hat{y}\times\hat{V}$$  
    
    4. Match **Quaternion** $Q_{VW}$ of **Visual Wall** $VW$ and **Quaternion** $Q_{VF}$ of **Visual Floor** $VF$ to **Projected Unit Vector** $\hat{V}$ direction using Unity $Quaternion.LookRotation$ method. 

    \\\\ Please Expand this to actual formula instead of Unity predefinded method
    $$Q_{VW} = Quaternion.LookRotation(\hat{V},\hat{y})$$ 
    $$Q_{VF} = Q_{VW}$$ 

    5. Get **Current User Relative Angle** $\theta_c$ from z component $\hat{V_z}$ and x component $\hat{V_x}$ of **Project Vector** in $-\pi$ to $\pi$ scale. Get $\theta_{diff}$
    $$\theta_c = arctan2(\hat{V_z},\hat{V_x})$$
    $$\text{diff}_x = \cos(\theta_p)\sin(\theta_c) - \sin(\theta_p)\cos(\theta_c)$$
    $$\text{diff}_y = \cos(\theta_p)\cos(\theta_c) + \sin(\theta_p)\sin(\theta_c)$$
    $$\theta = \theta + arctan2(\text{diff}_y,\text{diff}_x)$$
    $$\theta_p = \theta_c$$
    
    6. Get **User Travel Distance** $td$ from **Total Traveled Angle** $\theta$ multiplied by sum of **Radius** $r$ of **Haptic Wall** and **Initial Distance** $d$.
    $$td = \theta\cdot(r + d)$$
    
    7. Get the **Shifting Direction Vector** $\vec{S}$, which is oppsite to expected walking direction of **User**, which as clockwize **Tangent Unit Vector** $\hat{T}$, in **Travel Distance Magnitude** $td$.
    $$\vec{S} = td\cdot\hat{T}$$

    8. Set **Virtual Wall** $VW$ and **Visual Floor** $VF$ position to where **Projected Point** $P$ is shifted with **Shifting Direction Vector** $\vec{S}$. so the user is feeling as if they are walking on straight path, event though they were walking along surface of **Haptic Wall**.
    $$VW = P + \vec{S}$$
    $$VF = VW$$ 
    $$VW = \langle VW_x,h/2,VW_z \rangle$$
2. **Start Position Indicator** $SL$ and **End Position Indicator** $EL$, **User** $U$ guiding objects are calculated  by:
    1. Set **Start Position Indicator** $SL$ to where **Projected Point** $P$ is shifted with **Shifting Direction Vector** $\vec{S}$ and **Projected Unit Vector** $\hat{V}$ in **Initial Distance** $d$ magnitude.
    $$SL = P + \vec{S} + d \cdot \hat{V}$$

    2. Set **End Position Indicator** $EL$ to where **Start Position Indicator** $SL$ is shifted with anti-clockwize **Tangent Unit Vector** $-\hat{T}$ in **Expected Travel Distance** $p$.
    $$EL = SL - p \cdot \hat{T}$$

3. **Visual Left Hand**, calculated after **Visual Wall** calculation done:
    1. if **Visual Wall** $VW$ is in between **Actual Left Hand** $AH$ and **User** $U$ position, then **Visual Left Hand** $VH$ position is projected on closet point on surface of **Visual Wall** $VW$ from **Actual Left Hand** $AH$ position.
    Else, **Visual Left Hand** $VH$ stays at **Actual Left Hand** $AH$ position.
    $$
    \vec{AP} = (AH - P)
    $$
    $$VH=\begin{cases}
        AH & \text{if } (\vec{AP} \cdot \hat{V}) > 0 \\
        AH - \left(\vec{AP} \cdot \hat{V}\right) \cdot \hat{V} & \text{otherwise} 
    \end{cases}$$
4. Check If User Reached the Final Destionation:
    1. if **User Travel Distance** $td$ larger than **Expected Travel Distance** $p$, then terminate the experiment.
    $$\begin{array}{} \textbf{Terminate Experiment} & \text{if } td > p\\\end{array}$$
# Haptic Rendering Algorithm
called first vision rendering algorithm
1. **Haptic Left Hand** $HH$ calculated after **Visual Wall** $VW$ calculation done:
    1. if **Actual Left Hand** $AH$ is inside **Haptic Wall** $HW$, then **Haptic Left Hand** $HH$ position is projected on closet point on surface of **Haptic Wall** from **Actual Left Hand** $AH$ position. 
    Else, **Haptic Left Hand** $HH$ stays at **Actual Left Hand** $AH$ position
    $$ \vec{AW} = \langle AH_x,0,AH_z \rangle - \langle HW_x,0,HW_z \rangle$$
    $$ \hat{AW} = \frac{\vec{AW}}{\left|AW\right|}$$
    $$
    HH = \begin{cases}
        AH & \text{if } |AW| > r \\
        \langle HW_x,AH_y,HW_z \rangle + r \cdot \hat{AW}  & \text{otherwise}
    \end{cases}
    $$
    
    2. After **Haptic Left Hand** $HH$ calculation done, distance between **Actual Left Hand** $AH$ and **Haptic Left Hand** $HH$ will be sent to my device to display force based on its magnitude.
    $$ Force =  |HH - AH| $$

# Issues
1. HW seems to be too rough, mismatch of surface of HW and HS might be result of it
2. Interaction Screens with Image not yet implemented