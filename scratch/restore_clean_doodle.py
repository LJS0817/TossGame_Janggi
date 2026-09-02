import cv2
import numpy as np

# Load original doodle logo
img = cv2.imread('Assets/UI/janggi_doodle_logo.jpg')
h, w = img.shape[:2]

# 1. First remove all spirals and background dots using seamless inpainting
mask_spirals = np.zeros((h, w), dtype=np.uint8)
cv2.circle(mask_spirals, (140, 360), 65, 255, -1)
cv2.circle(mask_spirals, (560, 150), 55, 255, -1)
cv2.circle(mask_spirals, (470, 180), 30, 255, -1)
cv2.circle(mask_spirals, (865, 405), 35, 255, -1)
cv2.circle(mask_spirals, (150, 680), 65, 255, -1)
cv2.circle(mask_spirals, (870, 680), 55, 255, -1)
cv2.circle(mask_spirals, (865, 610), 45, 255, -1)
cv2.circle(mask_spirals, (340, 735), 25, 255, -1)
cv2.circle(mask_spirals, (660, 740), 25, 255, -1)
# also remove creature parts that extend into empty parchment background
cv2.rectangle(mask_spirals, (400, 170), (600, 250), 255, -1)
cv2.rectangle(mask_spirals, (200, 180), (320, 230), 255, -1)
cv2.rectangle(mask_spirals, (640, 180), (740, 230), 255, -1)
cv2.circle(mask_spirals, (860, 320), 45, 255, -1)
cv2.circle(mask_spirals, (780, 270), 45, 255, -1)

res = cv2.inpaint(img, mask_spirals, 9, cv2.INPAINT_NS)

# Colors from the original artwork
color_brown = (18, 40, 72)        # Dark brown ink outline
color_cream = (212, 236, 250)     # Cream card border / octagon face
color_terracotta = (76, 128, 212) # Terracotta orange (BGR)
color_sage = (108, 164, 158)      # Sage green (BGR)

# 2. Draw clean Top-Left Card (Terracotta)
# Card vertices: [TL, TR, BR, BL]
card_tl_outer = np.array([[142, 204], [376, 267], [320, 480], [86, 417]], dtype=np.int32)
card_tl_inner = np.array([[160, 222], [358, 275], [308, 460], [110, 407]], dtype=np.int32)

# Fill card body with cream border
cv2.fillPoly(res, [card_tl_outer], color_cream)
cv2.polylines(res, [card_tl_outer], True, color_brown, 6, cv2.LINE_AA)
# Fill inner card with terracotta
cv2.fillPoly(res, [card_tl_inner], color_terracotta)
cv2.polylines(res, [card_tl_inner], True, color_brown, 3, cv2.LINE_AA)

# Add simple charming card rune (concentric circle / sun motif like other cards)
cv2.circle(res, (245, 345), 36, color_cream, -1, cv2.LINE_AA)
cv2.circle(res, (245, 345), 36, color_brown, 4, cv2.LINE_AA)
cv2.circle(res, (245, 345), 20, color_terracotta, -1, cv2.LINE_AA)
cv2.circle(res, (245, 345), 20, color_brown, 3, cv2.LINE_AA)

# 3. Draw clean Top-Right Card (Sage Green)
card_tr_outer = np.array([[646, 168], [880, 231], [824, 444], [590, 381]], dtype=np.int32)
card_tr_inner = np.array([[664, 186], [862, 239], [812, 424], [614, 371]], dtype=np.int32)

# Fill card body with cream border
cv2.fillPoly(res, [card_tr_outer], color_cream)
cv2.polylines(res, [card_tr_outer], True, color_brown, 6, cv2.LINE_AA)
# Fill inner card with sage green
cv2.fillPoly(res, [card_tr_inner], color_sage)
cv2.polylines(res, [card_tr_inner], True, color_brown, 3, cv2.LINE_AA)

# Add card emblem (crown / lotus motif)
cv2.circle(res, (735, 305), 36, color_cream, -1, cv2.LINE_AA)
cv2.circle(res, (735, 305), 36, color_brown, 4, cv2.LINE_AA)
# 3 little leaves
cv2.ellipse(res, (735, 303), (10, 18), 0, 0, 360, color_sage, -1, cv2.LINE_AA)
cv2.ellipse(res, (735, 303), (10, 18), 0, 0, 360, color_brown, 3, cv2.LINE_AA)
cv2.ellipse(res, (724, 308), (7, 14), -30, 0, 360, color_sage, -1, cv2.LINE_AA)
cv2.ellipse(res, (724, 308), (7, 14), -30, 0, 360, color_brown, 2, cv2.LINE_AA)
cv2.ellipse(res, (746, 308), (7, 14), 30, 0, 360, color_sage, -1, cv2.LINE_AA)
cv2.ellipse(res, (746, 308), (7, 14), 30, 0, 360, color_brown, 2, cv2.LINE_AA)

# 4. Clean Octagon Top Face & Bevels (Overlapping on top of cards)
# Clean the top triangle of the octagon face where paws were
mask_top_face = np.zeros((h, w), dtype=np.uint8)
pts_top_oct = np.array([
    [420, 325], [590, 325], [696, 431], [580, 420], [430, 420], [314, 431]
], dtype=np.int32)
cv2.fillPoly(res, [pts_top_oct], color_cream)

# Draw the crisp dark brown octagon border lines
# Top horizontal edge
cv2.line(res, (425, 326), (585, 326), color_brown, 6, cv2.LINE_AA)
# Top-Left diagonal
cv2.line(res, (304, 447), (425, 326), color_brown, 6, cv2.LINE_AA)
# Top-Right diagonal
cv2.line(res, (585, 326), (706, 447), color_brown, 6, cv2.LINE_AA)

# Inner decorative border line of the octagon
cv2.line(res, (435, 337), (575, 337), color_brown, 2, cv2.LINE_AA)
cv2.line(res, (318, 451), (435, 337), color_brown, 2, cv2.LINE_AA)
cv2.line(res, (575, 337), (692, 451), color_brown, 2, cv2.LINE_AA)

# Save the restored crisp image
cv2.imwrite('Assets/UI/janggi_clean_doodle.jpg', res)
print('Masterpiece clean doodle saved successfully!')
