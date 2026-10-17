# دليل النشر على MonsterASP (IIS)

هذا الدليل يشرح كيفية بناء الحزمة ورفعها على استضافة MonsterASP.NET (خطة مجانية / IIS) لعرض النظام على العملاء.

## نظرة عامة على البنية

- الواجهة: تصدير ثابت لـ Next.js يُوضع **في جذر الموقع** (`wwwroot`) وليس في مجلد فرعي.
- الخادم: مكتبة `pos-server.dll` داخل `bin/`، تعمل عبر OWIN على IIS (Microsoft.Owin.Host.SystemWeb).
- قاعدة البيانات: SQLite في `data/dev.db` (داخل جذر الموقع).
- الهجرات (Migrations): ملفات SQL في `Migrations/` بجذر الموقع.
- الـ API يعمل على نفس النطاق (`same-origin`) — لا حاجة لـ CORS في الاستضافة.

## إعدادات `Web.config`

كل المفاتيح في `<appSettings>`. متغيّرات البيئة (env) لها الأولوية على `appSettings`.
الاستضافة لا تحمّل ملف `.env` — استخدم `Web.config` أو متغيّرات بيئة حقيقية.

| المفتاح | الافتراضي في الحزمة | الوظيفة |
|---|---|---|
| `POS_DB_PATH` | فارغ (→ `data/dev.db`) | مسار قاعدة البيانات. عيّنه لو الاستضافة تمنع الكتابة في الجذر |
| `POS_ENABLE_CORS` | فارغ (مقفول) | تفعيل CORS (غير مطلوب — same-origin) |
| `POS_SERVER_PRINTING` | `0` | الطباعة المباشرة من الخادم. مقفول على الاستضافة (لا توجد طابعات) — الـ API يردّ 503 |
| `POS_BACKUP_WORKER` | `0` | مؤقّت النسخ الاحتياطي داخل العملية. يُفضَّل external scheduler على الاستضافة |
| `POS_LOG_DIR` | `data\logs` | مجلد ملفات اللوج اليومية (`pos-YYYYMMDD.log`، يُحذف الأقدم من 14 يومًا). مقفول لو فارغ |
| `POS_LICENSE_SECRET` | فارغ | سر الترخيص كبديل عن ملف `license.secret` |
| `PRINTER_NAME` | فارغ | اسم الطابعة (يتجاهل لو `POS_SERVER_PRINTING=0`) |

> تحت IIS تُسجَّل كل نداءات `Console.*` (~194 موضع) في `POS_LOG_DIR` تلقائيًا.

## 1) بناء الحزمة

على جهاز التطوير (Windows):

```powershell
powershell -ExecutionPolicy Bypass -File deploy\iis\package.ps1
```

السكربت يقوم بـ:

1. بناء الواجهة مع `NEXT_PUBLIC_API_URL=__SAME_ORIGIN__` (يتحول إلى نص فارغ داخل الكود — نفس النطاق).
2. بناء الخادم كمكتبة (`-p:OutputType=Library`) إلى `deploy\iis\payload\bin\`.
3. نسخ التصدير إلى `deploy\iis\payload\` (جذر الموقع).
4. نسخ `Migrations\` و`Web.config` ومكتبات SQLite لكل من x64 وx86.
5. التحقق: لا يجوز أن يحتوي أي ملف `.js` على `localhost:3001`.

خيارات مفيدة:

```powershell
- SkipFrontend   # إعادة استخدام out/ الحالية
- SkipBackend    # (يتطلب بناء الخادم أولاً — السكربت يمسح payload كل مرة)
```

## 2) الاختبار المحلي قبل الرفع

```powershell
powershell -ExecutionPolicy Bypass -File deploy\iis\smoke-test.ps1
# اختبار بنسخة 32-bit (مهم لأن SQLite native يختلف):
powershell -ExecutionPolicy Bypass -File deploy\iis\smoke-test.ps1 -IisExpress "C:\Program Files (x86)\IIS Express\iisexpress.exe"
```

يجب أن تظهر جميع الفحوصات `PASS` (يشمل ذلك منع الوصول إلى `/data/dev.db` و`/Migrations/*.sql`).

تحقق شامل قبل الالتزام بالتغييرات:

```bash
npx tsc --noEmit && npm run lint && npm test && npm run build
```

## 3) تجهيز قاعدة البيانات وملف الرفع

**الترتيب مهم**: `package.ps1` بيمسح `payload\` كل مرة، والـ `smoke-test.ps1` بيحذف `data\` اللي هو نفسه أنشأها.
فـ جهّز الـ DB **بعد** الاختبار مباشرة.

```powershell
# 1) قاعدة بيانات مُهاجرَة مسبقًا (41 migration + المستخدم admin/1234)
powershell -ExecutionPolicy Bypass -File deploy\iis\prepare-db.ps1

# 2) ملف ZIP جاهز للرفع — جذره هو جذر الموقع
powershell -ExecutionPolicy Bypass -File deploy\iis\make-site-zip.ps1
```

الناتج: `deploy\iis\pos-site.zip` (~7.3 MB) بتحقق من نفسه:

- **ممنوع** وجود مجلد غلاف `payload/` (غلطة شائعة بتودّي الموقع لـ `wwwroot/payload/` فيطلع 404)
- **ممنوع** فواصل `\` في أسماء الملفات داخل الـ ZIP
- يتأكد إن `data\dev.db` متضمّن ولا لأ

### ليه تجهّز الـ DB مسبقًا؟

- الهجرات الـ 41 تتطبّق على جهازك — أول طلب على الاستضافة بيفتح على طول
- تقدر تseed بيانات عرض قبل ما توريها للعميل
- الـ smoke test بيتأكد إن الاستضافة **هتعيد استخدام** الـ DB من غير إعادة هجرات
  (فحص `pre-migrated database reused`)

> ⚠️ **مش بيلغي مشكلة الصلاحيات**: SQLite بيتكتب فيها مع كل عملية بيع/إضافة،
> فمجلد الموقع **لازم** يكون قابل للكتابة في كل الأحوال.

اختياري: لو مش عايز DB متبني، اスキّب `prepare-db.ps1` — الخادم بينشئها عند أول طلب تلقائيًا.

## 4) الرفع إلى الاستضافة

1. أنشئ الموقع من لوحة تحكم MonsterASP: **Websites ← Create Website** (اختيار الخطة Free أو Premium).
   ملاحظة: القائمة تُسمّى "Website" وليس "IIS site" — ما تبقاش تدوّر على مصطلح IIS.
2. ارفع **محتويات** `deploy\iis\payload\` كاملة إلى مجلد الموقع (`wwwroot`) بأحد الطريقتين:
   - **File Manager**: أيقونة `Files` في تفاصيل الموقع ← Upload.
   - **FTP/SFTP**: من القائمة اليسرى `Deploy (FTP/WebDeploy/Git)` —
     Host = `siteXXXX.siteasp.net`، Username = `siteXXXX`، Port `21`.
   - **ZIP**: `Deploy ← How to deploy Website content from ZIP file`.
3. المحتوى النهائي لازم يكون **مباشر في `wwwroot`** (مش في مجلد فرعي داخله):
   `Web.config`, `bin/`, `Migrations/`, `ar/`, `_next/`, صفحة `404/` وغيرها —
   يعني لو رفعت ملف ZIP مضغوط فيه مجلد، افتحه وارفع اللي جوّاه.
4. تأكد أن مجلد الموقع قابل للكتابة لحساب موقع IIS (لإنشاء `data\dev.db`).
5. إعدادات الإطار: اضبط إصدار **.NET Framework 4.x** للموقع من إعدادات لوحة التحكم
   (وضع Integrated pipeline هو الافتراضي عند MonsterASP).

## 5) ما بعد الرفع (قبل عرضه للعميل)

1. افتح `https://<نطاقك>/health` — يجب أن يعيد `200`.
2. سجّل الدخول بالحساب المبدئي: **`admin` / `1234`** (زرار التغيير الإجباري معطّل بموجب `040`).
3. **غيّر كلمة المرور فوراً**: الإعدادات ← المستخدمون ← المدير.
4. الترخيص: تجربة 14 يومًا. لعرض دائم دون قفل شاشة، أنشئ ملف `license.secret` في جذر الموقع (بجانب `Web.config`) وضع فيه سر الترخيص، أو عيّن متغيّر البيئة `POS_LICENSE_SECRET`. من واجهة الإعدادات ← الترخيص تحقق أن الحالة "مفتوحة".
5. أنشئ بيانات العرض (منتجات/فروع) أو استخدم باقة التهيئة الافتراضية إن وُجدت.

> تنبيه: مسار `data/dev.db` هو المسار الافتراضي. إن رفضت الاستضافة الكتابة في الجذر، عيّن `POS_DB_PATH` إلى مجلد مسموح به في `Web.config` (`appSettings`).

## 6) الطباعة

نقطة الطباعة المباشرة (`/api/printing/print` و`/api/printing/print-barcode`) معطّلة على الاستضافة عبر
`POS_SERVER_PRINTING=0` وتردّ بـ `503` برسالة واضحة — لا محاولة لاستدعاء طابعة غير موجودة على السيرفر.
الطلب من الفاتورة يُبتلع بصمت، وطباعة الباركود تعرض toast خطأ في الواجهة.

للمرحلة التالية: وكيل طباعة محلي (Print Agent) على جهاز العميل يتصل بالطابعة ويسحب طابور الطباعة من الخادم.

## 7) استكشاف الأخطاء

| العَرَض | الخطوة |
|---|---|
| خطأ 500.19 | إعداد `Web.config` غير صالح — راجع تفاصيل الخطأ (غالباً عنصر مكرر مثل `hiddenSegments`) |
| خطأ 500.21/500.19 عند التشغيل | تأكد من وجود `bin\` كامل و`Microsoft.Owin.Host.SystemWeb` |
| الصفحة الرئيسية فارغة/404 | تأكد أن `ar/index.html` موجود في جذر الموقع — لو لقيت `wwwroot/payload/` رفعت ZIP بغلاف، انقل المحتوى لـ `wwwroot` |
| فشل الهجرات/بدء الخادم | راجع `data\startup-error.txt` |
| لا تُحمّل ملفات JS | تأكد من رفع مجلد `_next/` كاملاً وبأن `Web.config` يحتوي `mimeMap` لـ `.mjs` |
| أخطاء تشغيلية عامة | راجع `data\logs\pos-YYYYMMDD.log` (كل `Console.*` بيتسجّل هناك تحت IIS) |
| خطأ 503 عند الطباعة | متوقع — `POS_SERVER_PRINTING=0` في `Web.config` |

## 8) التحديث المستقبلي

أعد بناء الحزمة (الخطوة 1)، اختبرها (2)، ثم جهّز الـ DB والـ ZIP (3) — وارفع `pos-site.zip` فوق القديم مع
**الإبقاء على** `data/` و`license.secret` في الموقع. لو تركت `data/dev.db` كما هي، الهجرات الجديدة تُطبَّق
تلقائيًا عند أول طلب (شوف `prepare-db.ps1` لو عايز تعيد التجهيز محليًا).
