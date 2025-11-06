import { initializeApp } from 'firebase/app'
import { getMessaging, getToken, onMessage, Messaging } from 'firebase/messaging'

const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY || 'mock-key',
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN || 'vibora-mvp.firebaseapp.com',
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID || 'vibora-mvp-mock',
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID || '12345',
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID || '1:12345:web:abc',
}

const app = initializeApp(firebaseConfig)

let messaging: Messaging | null = null

if (typeof window !== 'undefined' && 'serviceWorker' in navigator) {
  try {
    messaging = getMessaging(app)
  } catch (error) {
    console.warn('Firebase Messaging initialization failed:', error)
  }
}

export { messaging, getToken, onMessage }
export default app
