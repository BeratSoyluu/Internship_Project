export interface VbAccountDetail {
  paraBirimi: string;                 // "TL", "USD"...
  sonIslemTarihi: string;             // ISO veya "YYYY-MM-DD HH:mm:ss"
  hesapDurumu: 'A' | 'K';             // A: Açık, K: Kapalı
  acilisTarihi: string;               // ISO
  iban: string;
  musteriNo: string;
  bakiye: number;                     // kuruşsuz
  hesapTuru: 1 | 2 | 3 | 4;           // tip kodu
  subeKodu: string;
  hesapNo: string;
}

export const HESAP_DURUMU_MAP: Record<'A' | 'K', string> = {
  A: 'Açık',
  K: 'Kapalı',
};

export const HESAP_TURU_MAP: Record<1 | 2 | 3 | 4, string> = {
  1: 'Vadeli Türk Parası Mevduat Hesabı',
  2: 'Vadesiz Türk Parası Mevduat Hesabı',
  3: 'Vadeli Yabancı Para Mevduat Hesabı',
  4: 'Vadesiz Yabancı Para Mevduat Hesabı',
};
