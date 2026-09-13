# Project Guidelines & Rules

1. **Long-term Efficiency First**: เสนอและเลือกทางเลือกที่มีประสิทธิภาพสูงสุดในระยะยาวเสมอ ทั้งด้าน Architecture, Performance และ Maintainability
2. **Transparent Execution**: อธิบายเสมอว่ากำลังทำอะไรอยู่ ทำเพื่ออะไร และมีขั้นตอนอย่างไร
3. **No Auto Commit/Push**: ห้ามทำ `git commit` หรือ `git push` ด้วยตัวเองเด็ดขาด ต้องส่งให้ผู้ใช้ตรวจและได้รับการยืนยันก่อนเสมอ
4. **Clean Architecture & Anti-Spaghetti**: หลีกเลี่ยงการสร้างไฟล์ที่ไม่จำเป็นหรือทำให้เกิด Spaghetti Code แต่สร้างไฟล์ใหม่ได้เมื่อเป็นการจัดโครงสร้างเพื่อความอ่านง่าย ระเบียบ และ Maintainability ที่ดีขึ้น
5. **Explicit Approval Before Code Edits**: ห้ามแก้ไขโค้ดหรือไฟล์ในโปรเจกต์โดยทันที ต้องอธิบายรายละเอียดการเปลี่ยนแปลงและรอได้รับการยืนยันจากผู้ใช้ก่อนลงมือแก้ไขเสมอ
6. **SOLID First, Especially SRP**: นี่คือโปรเจกต์ระยะยาว ก่อนสร้างหรือวางแผนอะไรใหม่ ต้องคิดบนหลัก SOLID ก่อนเสมอ โดยเฉพาะ Single Responsibility Principle — แต่ละ class ต้องมีหน้าที่เดียวที่ชัดเจน ห้ามแตก class ย่อยมั่วจนกระจัดกระจายเกินจำเป็น และต้องออกแบบให้แก้ไขหรือต่อยอดได้ง่ายในระยะยาว

# Ingredient Prefab Convention

วัตถุดิบหนึ่งหน่วย (นับ/สุก/เข้าสูตร) = `IngredientInstance` หนึ่งตัวที่ root และมีชิ้นฟิสิกส์ได้ 1..N ชิ้น ทุกระบบอ่านชิ้นจาก `IngredientInstance.Bodies` ห้ามหา `Rigidbody` เอง

```
<ingredient>-pf      IngredientInstance + IngredientVisuals        ← ตัวตน
 ├─ piece_1          MeshRenderer + Collider + Rigidbody           ← ชิ้นฟิสิกส์
 └─ piece_N          ...
```

- วัตถุดิบชิ้นเดียว: root มี Rigidbody + Collider เองได้ (root = ชิ้นเดียว)
- วัตถุดิบหลายชิ้น (หั่น/เต๋า): **ห้าม** มี Rigidbody หรือ Collider ที่ root — Rigidbody ซ้อนกันจะสู้กันเอง `IngredientInstance.Awake` จะ log error ถ้าพบ
- ชิ้นลูกห้ามมี `IngredientInstance`/`IngredientVisuals` ของตัวเอง ไม่งั้นถูกนับเป็นวัตถุดิบเพิ่ม
- รูปแบบต่างกัน (whole / diced / ground) = `IngredientDataSO` คนละตัว แต่ละตัวมี prefab ของตัวเอง ไม่ต้องเพิ่ม enum
