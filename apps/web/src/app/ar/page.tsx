import { Badge, Card } from "@atiqsalon/ui";

export const metadata = {
  title: "AtiqSalon AI بالعربية",
  description: "نظام تشغيل متكامل لأعمال الجمال والعافية",
};

export default function ArabicHome() {
  return (
    <main dir="rtl" lang="ar" className="rtl">
      <section className="hero">
        <div className="container hero-grid">
          <div>
            <Badge>AtiqSalon AI</Badge>
            <h1 style={{ fontFamily: "var(--font-display)" }}>
              أدِر صالونك بالكامل بالذكاء الاصطناعي
            </h1>
            <p className="lead">
              منصة واحدة لإدارة المواعيد والعملاء والموظفين والمدفوعات والمخزون
              والتسويق وأداء الأعمال.
            </p>
            <div className="actions">
              <a className="button" href="http://localhost:3001/register">
                ابدأ التجربة المجانية
              </a>
              <a className="button secondary" href="/book-demo">
                احجز عرضاً توضيحياً
              </a>
            </div>
          </div>
          <Card>
            <p className="eyebrow">جاهز للمنطقة</p>
            <h2>العربية والإنجليزية من الأساس</h2>
            <p>
              تخطيط من اليمين إلى اليسار، مع دعم الإمارات والدرهم الإماراتي
              وتوقيت آسيا/دبي.
            </p>
          </Card>
        </div>
      </section>
    </main>
  );
}
