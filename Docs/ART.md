# 첫 농장 픽셀 그래픽

생성 방식: Codex 내장 imagegen. 새로 생성한 임시 이미지이며 스타듀밸리 원본 이미지는 사용하지 않았다.
프로젝트 원본: `Assets/Resources/FarmAtlas.png`. 투명 배경, 4×4 배열, Point 필터, 무압축, 밉맵 없음.
행 순서: 캐릭터 아래/위/왼쪽/오른쪽; 삽/물뿌리개/돌/구름; 보관 칸/잔디/흙/젖은 흙; 새싹/나무/꽃/돌 식물.
FarmArt에서 스프라이트와 핫바 아이콘에 같은 원본을 사용한다. 이미지는 시제품용이며 픽셀 크기와 타일 경계는 최종 미술 작업 때 정리할 수 있다.

## 사용한 최종 프롬프트

Create an original pixel art sprite atlas for a cozy 2D top-down farming game. Transparent background. EXACTLY a 4-column by 4-row grid, equal square cells, no gutters, total image square. Each sprite is centered and wholly contained inside its own cell, generous transparent padding around characters and objects. Crisp hard pixel edges, limited warm earthy colors, no outlines of cells, no text, no shadows crossing cells, no watermark. Read cells left to right, top to bottom: row 1 farmer facing down, same farmer facing up, same farmer facing left, same farmer facing right. Farmer wears straw hat, green shirt, brown boots, readable small game sprite with top-down oblique view. Row 2 shovel, watering can, small stone, fluffy small cloud. Row 3 wooden empty inventory square frame, seamless green grass tile filling the entire cell, tilled brown soil tile filling cell, watered dark brown soil tile filling cell. Row 4 small green sprout, leafy small tree, yellow wildflower clump, magical leafy plant carrying a stone. Objects must have consistent overhead oblique game perspective. Original designs, no existing game characters or assets. Grass and soil are simple restrained textures with broad readable areas, not busy.
