import { useEffect, useState } from 'react';

import styles from './AdCarousel.module.css';

interface AdCarouselProps {
  /** Public image URLs configured per company. */
  images: string[];
}

/** Auto-rotating advertising banner for the company's promotions. */
export function AdCarousel({ images }: AdCarouselProps) {
  const [index, setIndex] = useState(0);

  useEffect(() => {
    if (images.length <= 1) return;
    const id = window.setInterval(() => {
      setIndex((current) => (current + 1) % images.length);
    }, 4500);
    return () => window.clearInterval(id);
  }, [images.length]);

  if (images.length === 0) return null;

  return (
    <div className={styles.carousel}>
      {images.map((src, i) => (
        <img
          key={src}
          src={src}
          alt=""
          className={[styles.slide, i === index ? styles.active : ''].filter(Boolean).join(' ')}
        />
      ))}

      {images.length > 1 && (
        <div className={styles.dots}>
          {images.map((src, i) => (
            <span
              key={src}
              className={[styles.dot, i === index ? styles.dotActive : ''].filter(Boolean).join(' ')}
            />
          ))}
        </div>
      )}
    </div>
  );
}
