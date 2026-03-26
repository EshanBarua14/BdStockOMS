// src/store/useSelectedBOStore.ts
import { create } from 'zustand'

export interface BOClient {
  userId: number
  boNumber: string
  fullName: string
  accountType: string
  cashBalance: number
  availableMargin: number
}

interface SelectedBOStore {
  selectedBO: BOClient | null
  setSelectedBO: (bo: BOClient | null) => void
}

export const useSelectedBOStore = create<SelectedBOStore>((set) => ({
  selectedBO: null,
  setSelectedBO: (bo) => set({ selectedBO: bo }),
}))
