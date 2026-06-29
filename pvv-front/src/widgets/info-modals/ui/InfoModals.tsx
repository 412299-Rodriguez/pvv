import { useState } from 'react';

import { usePvvConfigStore } from '@/entities/company';
import { useInfoModalStore } from '@/shared/lib';

import styles from './InfoModals.module.css';

/** Renders the active info modal (Terms / Privacy legal, or FAQ). */
export function InfoModals() {
  const open = useInfoModalStore((s) => s.open);
  const close = useInfoModalStore((s) => s.close);

  if (open === null) return null;

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      onClick={(e) => {
        if (e.target === e.currentTarget) close();
      }}
    >
      <div className={styles.modal}>
        <button type="button" className={styles.closeBtn} onClick={close} aria-label="Cerrar">
          ✕
        </button>
        {open === 'faq' ? <FaqContent /> : <LegalContent initial={open} />}
      </div>
    </div>
  );
}

function LegalContent({ initial }: { initial: 'terms' | 'privacy' }) {
  const termsText = usePvvConfigStore((s) => s.termsText);
  const privacyText = usePvvConfigStore((s) => s.privacyText);
  const [tab, setTab] = useState<'terms' | 'privacy'>(initial);

  return (
    <>
      <div className={styles.tabs}>
        <button
          type="button"
          className={tab === 'terms' ? styles.tabActive : styles.tab}
          onClick={() => setTab('terms')}
        >
          Términos y Condiciones
        </button>
        <button
          type="button"
          className={tab === 'privacy' ? styles.tabActive : styles.tab}
          onClick={() => setTab('privacy')}
        >
          Política de Privacidad
        </button>
      </div>
      <div className={styles.body}>{tab === 'terms' ? termsText : privacyText}</div>
    </>
  );
}

function FaqContent() {
  const faqs = usePvvConfigStore((s) => s.faqs);
  const [openIndex, setOpenIndex] = useState<number | null>(0);

  return (
    <>
      <h2 className={styles.title}>Preguntas frecuentes</h2>
      <div className={styles.faqList}>
        {faqs.map((faq, index) => (
          <div key={faq.question} className={styles.faqItem}>
            <button
              type="button"
              className={styles.faqQuestion}
              onClick={() => setOpenIndex(openIndex === index ? null : index)}
            >
              <span>{faq.question}</span>
              <span className={styles.faqChevron}>{openIndex === index ? '−' : '+'}</span>
            </button>
            {openIndex === index && <div className={styles.faqAnswer}>{faq.answer}</div>}
          </div>
        ))}
      </div>
    </>
  );
}
