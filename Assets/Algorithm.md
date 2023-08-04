# Want to Verify
* Impact of active repelling kinesthetic feedback on redirected walking(RDW).
* If my haptic device is improving RDW's physical spatial requirement.

# Conditions
1. Visual manipulation only (RDW)
2. Visual manipulation with kinesthetic force applied (RDW + stimuli)

# Initialization Algorithm
1. **Haptic Wall** $HW$, a cylindrical wall with **Radius** $r$ and **Height** $h$ on the **Left** $\hat{L}$ of the **User** $U$, where **Initial Comfortable Distance** $d$ between surface of the wall with the **User** is just enough to touch the wall with comfort. **Left** $\hat{L}$ and position of the **User** is measured.
$$HW =\langle U_x,h,U_z \rangle+(r+d)\cdot\hat{L} $$

# Vision Rendering Algorithm
1. **Visual Wall** $VW$, infinite straight wall on the surface of the **Haptic Wall** and left of the **User**, and **Visual Floor** $VF$ where its transformation is calculated by:
    1. Get **Projected Vector** $\vec{V}$ from center of **Haptic Wall** $HW$ to position of **User** $U$
    $$\vec{V} = U - HW$$
    $$\vec{V} = \langle \vec{V}_x,0,\vec{V}_z \rangle$$
    2. Get **Projected Point** $P$ by projecting **Projected Vector** $\vec{V}$ from center of **Haptic Wall** $HW$ in **Radius** $r$ magnitude.
    $$\hat{V} = \frac{\vec{V}}{\left|V\right|}$$
    $$P = HW+r\cdot\hat{V}$$ 
    $$P = (P_x,0,P_z)$$
    3. Get anti-clockwize **Tangent Unit Vector** $\hat{T}$ on the surface of **Haptic Wall** at **Projected Point** using **Projected Vector** $\vec{V}$, and absolute **Up** $\hat{y}$ direction.
    $$\hat{T} = \hat{V}\times\hat{y}$$  
    
    4. Match **Quaternion** $Q_{VW}$ of **Visual Wall** $VW$ and **Quaternion** $Q_{VF}$ of **Visual Floor** $VF$ to **Projected Unit Vector** $\hat{V}$ direction using Unity $Quaternion.LookRotation$ method. \\\\ Please Expand this to actual formula instead of Unity predefinded method
    $$Q_{VW} = Quaternion.LookRotation(\hat{T},\hat{y})$$ 
    $$Q_{VF} = Q_{VW}$$ 
    5. Get **User Relative Angle** $\theta$ from z component $\hat{V_z}$ and x component $\hat{V_x}$ of **Project Vector** in 0 to 2PI scale.
    $$\theta = (arctan2(\hat{V_z},\hat{V_x})+2\pi)\bmod(2\pi)$$
    6. Get **User Travel Distance** $td$ from **User Relative Angle** $\theta$ multiplied by sum of **Radius** $r$ of **Haptic Wall** and **Initial Distance** $d$.
    $$td = \theta\cdot(r + d)$$
    7. Get the **Shifting Direction Vector** $\vec{S}$, which is oppsite to expected walking direction of **User**, which as anti-clockwize **Tangent Unit Vector** $\hat{T}$, in **Travel Distance Magnitude** $td$.
    $$\vec{S} = td\cdot\hat{T}$$

    8. Set **Virtual Wall** $VW$ and **Visual Floor** $VF$ position to where **Projected Point** $P$ is shifted with **Shifting Direction Vector** $\vec{S}$. so the user is feeling as if they are walking on straight path, event though they were walking along surface of **Haptic Wall**.
    $$VW = P + \vec{S}$$
    $$VF = VW$$ 
    $$VW = \langle VW_x,h/2,VW_z \rangle$$

2. **Visual Left Hand**, calculated after **Visual Wall** calculation done:
    1. if **Visual Wall** $VW$ is in between **Actual Left Hand** $AH$ and **User** $U$ position, then **Visual Left Hand** $VH$ position is projected on closet point on surface of **Visual Wall** $VW$ from **Actual Left Hand** $AH$ position.
    Else, **Visual Left Hand** $VH$ stays at **Actual Left Hand** $AH$ position.
    $$
    \vec{AP} = (AH - P)
    $$
    $$VH=\begin{cases}
        AH & \text{if } (\vec{AP} \cdot \hat{V}) > 0 \\
        AH - \left(\vec{AP} \cdot \hat{V}\right) \cdot \hat{V} & \text{otherwise} 
    \end{cases}$$

# Haptic Rendering Algorithm

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
1. When User crosses the zero degree to 2pi degree in backward direction, the texture flips (Need Testing)
2. HW seems to be too rough, mismatch of surface of HW and HS might be result of it
3. Interaction Screens with Image not yet implemented
4. Destination need to be projected on based on Visual Wall, not only HW (Need Testing)