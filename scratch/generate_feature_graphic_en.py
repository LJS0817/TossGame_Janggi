import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

# ─────────────────────────────────────────────────────────────────────────────
# 4x Super-Sampling Anti-Aliasing (SSAA) for English Feature Graphic
# Render at 4096 x 2000, then downscale to 1024 x 500 using Lanczos resampling.
# ─────────────────────────────────────────────────────────────────────────────

SCALE = 4
FINAL_W, FINAL_H = 1024, 500
W, H = FINAL_W * SCALE, FINAL_H * SCALE

# 1. Base Clean Light Background Gradient (#F6F8FB -> #EDF3F8)
bg = np.zeros((H, W, 3), dtype=np.uint8)
for y in range(H):
    t = y / float(H)
    for x in range(W):
        s = x / float(W)
        r = int(248 * (1 - s) + 237 * s - 4 * t)
        g = int(250 * (1 - s) + 243 * s - 4 * t)
        b = int(252 * (1 - s) + 248 * s - 4 * t)
        bg[y, x] = [b, g, r] # BGR

# 2. Board Layer on the Right Side
board_mask = np.zeros((H, W), dtype=np.uint8)
board_pts = np.array([
    [int(520 * SCALE), int(55 * SCALE)],
    [int(985 * SCALE), int(35 * SCALE)],
    [int(1015 * SCALE), int(450 * SCALE)],
    [int(530 * SCALE), int(470 * SCALE)]
], dtype=np.int32)

# Soft shadow under board
shadow_mask = np.zeros((H, W), dtype=np.uint8)
board_shadow_pts = np.array([
    [int(520 * SCALE), int(60 * SCALE)],
    [int(995 * SCALE), int(40 * SCALE)],
    [int(1025 * SCALE), int(465 * SCALE)],
    [int(525 * SCALE), int(485 * SCALE)]
], dtype=np.int32)
cv2.fillPoly(shadow_mask, [board_shadow_pts], 180)
shadow_blurred = cv2.GaussianBlur(shadow_mask, (41, 41), 18)

for c in range(3):
    bg[:, :, c] = np.clip(bg[:, :, c].astype(np.int32) - (shadow_blurred.astype(np.int32) * 45 // 255), 0, 255).astype(np.uint8)

# Fill board surface with in-game board color #EDF3F8
board_surface = np.zeros((H, W, 3), dtype=np.uint8)
board_surface[:] = (248, 243, 237)

# Board grid lines
line_col = (162, 122, 75)
line_width = int(2 * SCALE)

for i in range(10):
    t = i / 9.0
    p1 = (int((540 * (1 - t) + 965 * t) * SCALE), int((72 * (1 - t) + 52 * t) * SCALE))
    p2 = (int((550 * (1 - t) + 995 * t) * SCALE), int((452 * (1 - t) + 432 * t) * SCALE))
    cv2.line(board_surface, p1, p2, line_col, line_width, cv2.LINE_AA)

for j in range(11):
    u = j / 10.0
    p1 = (int((540 * (1 - u) + 550 * u) * SCALE), int((72 * (1 - u) + 452 * u) * SCALE))
    p2 = (int((965 * (1 - u) + 995 * u) * SCALE), int((52 * (1 - u) + 432 * u) * SCALE))
    cv2.line(board_surface, p1, p2, line_col, line_width, cv2.LINE_AA)

# Palace crosses
p_top_tl = (int((540 * (1 - 0.33) + 965 * 0.33) * SCALE), int((72 + 15) * SCALE))
p_top_br = (int((540 * (1 - 0.66) + 965 * 0.66) * SCALE), int((72 + 105) * SCALE))
p_top_tr = (int((540 * (1 - 0.66) + 965 * 0.66) * SCALE), int((72 + 15) * SCALE))
p_top_bl = (int((540 * (1 - 0.33) + 965 * 0.33) * SCALE), int((72 + 105) * SCALE))
cv2.line(board_surface, p_top_tl, p_top_br, line_col, line_width, cv2.LINE_AA)
cv2.line(board_surface, p_top_tr, p_top_bl, line_col, line_width, cv2.LINE_AA)

p_bot_tl = (int((550 * (1 - 0.33) + 995 * 0.33) * SCALE), int(330 * SCALE))
p_bot_br = (int((550 * (1 - 0.66) + 995 * 0.66) * SCALE), int(420 * SCALE))
p_bot_tr = (int((550 * (1 - 0.66) + 995 * 0.66) * SCALE), int(330 * SCALE))
p_bot_bl = (int((550 * (1 - 0.33) + 995 * 0.33) * SCALE), int(420 * SCALE))
cv2.line(board_surface, p_bot_tl, p_bot_br, line_col, line_width, cv2.LINE_AA)
cv2.line(board_surface, p_bot_tr, p_bot_bl, line_col, line_width, cv2.LINE_AA)

cv2.fillPoly(board_mask, [board_pts], 255)
cv2.polylines(board_surface, [board_pts], True, (178, 116, 29), int(5 * SCALE), cv2.LINE_AA)

board_mask_aa = cv2.GaussianBlur(board_mask, (5, 5), 1.2)
for c in range(3):
    alpha = board_mask_aa.astype(np.float32) / 255.0
    bg[:, :, c] = (bg[:, :, c].astype(np.float32) * (1.0 - alpha) + board_surface[:, :, c].astype(np.float32) * alpha).astype(np.uint8)

img_pil = Image.fromarray(cv2.cvtColor(bg, cv2.COLOR_BGR2RGB))
draw = ImageDraw.Draw(img_pil, 'RGBA')

def S(val):
    return int(val * SCALE)

# Fonts
font_path_bold = "C:/Windows/Fonts/segoeuib.ttf"
font_path_korean = "C:/Windows/Fonts/malgunbd.ttf"

font_title = ImageFont.truetype(font_path_bold, 50 * SCALE)
font_subtitle = ImageFont.truetype(font_path_bold, 17 * SCALE)
font_category = ImageFont.truetype(font_path_bold, 13 * SCALE)
font_tagline = ImageFont.truetype(font_path_bold, 17 * SCALE)
font_badge = ImageFont.truetype(font_path_bold, 13 * SCALE)
font_subinfo = ImageFont.truetype(font_path_bold, 13 * SCALE)

font_hanja_large = ImageFont.truetype(font_path_korean, 84 * SCALE)
font_hanja_med = ImageFont.truetype(font_path_korean, 48 * SCALE)
font_hanja_piece = ImageFont.truetype(font_path_korean, 44 * SCALE)
font_card_cost = ImageFont.truetype(font_path_bold, 16 * SCALE)
font_card_sub = ImageFont.truetype(font_path_bold, 12 * SCALE)

# ── 3. LEFT BRANDING SECTION (ENGLISH) ──

# A. Category pill badge
draw.rounded_rectangle([(S(52), S(48)), (S(310), S(82))], radius=S(17), fill=(242, 247, 252, 255), outline=(165, 202, 230, 255), width=S(1.5))
draw.ellipse([(S(68), S(62)), (S(74), S(68))], fill=(29, 116, 178, 255))
draw.text((S(190), S(65)), "CARD SUMMON TACTICS", fill=(29, 116, 178, 255), font=font_category, anchor="mm")

# B. Main Title: JANGGI ARCADE
draw.text((S(50), S(110)), "JANGGI ARCADE", fill=(15, 23, 42, 255), font=font_title)

# C. Subtitle: K O R E A N   C H E S S   B A T T L E
draw.text((S(53), S(185)), "K O R E A N   C H E S S   B A T T L E", fill=(29, 116, 178, 255), font=font_subtitle)

# D. Feature Tagline Card
draw.rounded_rectangle([(S(52), S(234)), (S(470), S(286))], radius=S(14), fill=(0, 0, 0, 15))
draw.rounded_rectangle([(S(50), S(230)), (S(468), S(282))], radius=S(14), fill=(255, 255, 255, 255), outline=(226, 232, 240, 255), width=S(1))
draw.text((S(70), S(256)), "Summon pieces with cards to defend your King!", fill=(51, 65, 85, 255), font=font_tagline, anchor="lm")

# E. 3 Feature Badges
badges = [("Tactical Thrills", (29, 116, 178)), ("Deck Defense", (21, 115, 85)), ("Smart AI Battle", (186, 52, 52))]
bx = 50
for text, accent in badges:
    bbox_w = 135
    draw.rounded_rectangle([(S(bx + 1), S(317)), (S(bx + bbox_w + 1), S(357))], radius=S(10), fill=(0, 0, 0, 12))
    draw.rounded_rectangle([(S(bx), S(315)), (S(bx + bbox_w), S(355))], radius=S(10), fill=(255, 255, 255, 255), outline=(203, 213, 225, 255), width=S(1))
    draw.text((S(bx + bbox_w // 2), S(335)), text, fill=(30, 41, 59, 255), font=font_badge, anchor="mm")
    bx += bbox_w + 12

# Sub points
draw.text((S(52), S(395)), "• Traditional Korean Chess with tactical deckbuilding", fill=(100, 116, 139, 255), font=font_subinfo)
draw.text((S(52), S(420)), "• Fast-paced 1-minute casual battles anywhere, anytime", fill=(100, 116, 139, 255), font=font_subinfo)

# ── 4. RIGHT SIDE: IN-GAME PIECES & CARDS (ENGLISH SUBTITLES) ──

def draw_ingame_card(center_x, center_y, angle, card_cost, hanja, role_name, is_cho=True):
    card_w, card_h = S(120), S(180)
    card_img = Image.new("RGBA", (card_w + S(50), card_h + S(50)), (0, 0, 0, 0))
    cdraw = ImageDraw.Draw(card_img)
    
    color_primary = (29, 116, 178) if is_cho else (186, 52, 52)
    
    cdraw.rounded_rectangle([(S(25), S(30)), (S(25) + card_w, S(30) + card_h)], radius=S(14), fill=color_primary)
    cdraw.rounded_rectangle([(S(25), S(25)), (S(25) + card_w, S(25) + card_h)], radius=S(14), fill=(255, 255, 255, 255), outline=color_primary, width=S(2.5))
    
    cdraw.ellipse([(S(33), S(33)), (S(61), S(61))], fill=color_primary)
    cdraw.text((S(47), S(47)), str(card_cost), fill=(255, 255, 255, 255), font=font_card_cost, anchor="mm")
    
    cdraw.text((S(25) + card_w // 2, S(105)), hanja, fill=color_primary, font=font_hanja_med, anchor="mm")
    cdraw.text((S(25) + card_w // 2, S(155)), role_name, fill=(100, 116, 139, 255), font=font_card_sub, anchor="mm")
    
    rotated = card_img.rotate(angle, resample=Image.BICUBIC, expand=True)
    rw, rh = rotated.size
    img_pil.paste(rotated, (S(center_x) - rw // 2, S(center_y) - rh // 2), rotated)

# Hand Card 1: 車 (Chariot)
draw_ingame_card(640, 240, -18, 5, "車", "Chariot", is_cho=True)

# Hand Card 2: 包 (Cannon)
draw_ingame_card(880, 230, 16, 2, "包", "Cannon", is_cho=True)

def draw_ingame_piece(cx, cy, radius, text, is_cho=True, is_hero=False):
    color_primary = (29, 116, 178) if is_cho else (186, 52, 52)
    color_border = (165, 202, 230) if is_cho else (232, 185, 185)
    
    r = S(radius)
    center_x = S(cx)
    center_y = S(cy)
    
    pts = []
    for i in range(8):
        a = (i * 45 + 22.5) * np.pi / 180
        pts.append((center_x + r * np.cos(a), center_y + r * np.sin(a)))
    
    shadow_offset = S(12 if is_hero else 7)
    pts_shadow = [(px, py + shadow_offset) for px, py in pts]
    draw.polygon(pts_shadow, fill=color_primary)
    
    draw.polygon(pts, fill=(255, 255, 255, 255), outline=color_border)
    
    pts_inner = []
    r_inner = r * 0.90
    for i in range(8):
        a = (i * 45 + 22.5) * np.pi / 180
        pts_inner.append((center_x + r_inner * np.cos(a), center_y + r_inner * np.sin(a)))
    draw.polygon(pts_inner, outline=color_border)
    
    draw.arc([(center_x - r * 0.7, center_y - r * 0.9), (center_x + r * 0.7, center_y - r * 0.3)], start=200, end=340, fill=(255, 255, 255, 220), width=S(2))
    
    font_to_use = font_hanja_large if is_hero else font_hanja_piece
    draw.text((center_x, center_y), text, fill=color_primary, font=font_to_use, anchor="mm")

# Enemy AI Piece: Han King 漢
draw_ingame_piece(910, 105, 52, "漢", is_cho=False, is_hero=False)

# Ally Piece: Cho Knight 馬
draw_ingame_piece(595, 385, 46, "馬", is_cho=True, is_hero=False)

# HERO CENTRAL PIECE: Cho King / General 將
draw_ingame_piece(760, 255, 96, "將", is_cho=True, is_hero=True)

# ─────────────────────────────────────────────────────────────────────────────
# 5. DOWNSAMPLE 4x -> 1x via LANCZOS (Pure Vector Smoothness)
# ─────────────────────────────────────────────────────────────────────────────
final_img = img_pil.resize((FINAL_W, FINAL_H), resample=Image.Resampling.LANCZOS)

output_path = "Assets/UI/janggi_arcade_feature_graphic_en_1024x500.png"
final_img.save(output_path, format="PNG", optimize=True)
print(f"English 4x SSAA Feature Graphic saved successfully to: {output_path}")
