import { useTranslation } from 'react-i18next'

export const Instrument = {
  vocal: 0,
  guitar: 1,
  bass: 2,
  drums: 3,
  keys: 4,
  percussion: 5,
  winds: 6,
  strings: 7,
  electronic: 8,
  other: 9,
} as const
export type Instrument = (typeof Instrument)[keyof typeof Instrument]

export const SkillLevel = { beginner: 0, intermediate: 1, advanced: 2 } as const
export type SkillLevel = (typeof SkillLevel)[keyof typeof SkillLevel]

export const MusicianGoal = { original: 0, covers: 1, either: 2 } as const
export type MusicianGoal = (typeof MusicianGoal)[keyof typeof MusicianGoal]

export const MusicianCommitment = { justRehearse: 0, playLive: 1, record: 2 } as const
export type MusicianCommitment = (typeof MusicianCommitment)[keyof typeof MusicianCommitment]

export const Reach = { myCity: 0, abc: 1, greaterSaoPaulo: 2, anywhere: 3 } as const
export type Reach = (typeof Reach)[keyof typeof Reach]

export const WeekDay = { monday: 0, tuesday: 1, wednesday: 2, thursday: 3, friday: 4, saturday: 5, sunday: 6 } as const
export type WeekDay = (typeof WeekDay)[keyof typeof WeekDay]

export const DayPeriod = { morning: 0, afternoon: 1, night: 2 } as const
export type DayPeriod = (typeof DayPeriod)[keyof typeof DayPeriod]

export const OpeningStatus = { open: 0, filled: 1, closed: 2 } as const
export type OpeningStatus = (typeof OpeningStatus)[keyof typeof OpeningStatus]

export const InterestStatus = { pending: 0, invited: 1, dismissed: 2 } as const
export type InterestStatus = (typeof InterestStatus)[keyof typeof InterestStatus]

export const instrumentKey: Record<Instrument, string> = {
  [Instrument.vocal]: 'vocal',
  [Instrument.guitar]: 'guitar',
  [Instrument.bass]: 'bass',
  [Instrument.drums]: 'drums',
  [Instrument.keys]: 'keys',
  [Instrument.percussion]: 'percussion',
  [Instrument.winds]: 'winds',
  [Instrument.strings]: 'strings',
  [Instrument.electronic]: 'electronic',
  [Instrument.other]: 'other',
}

export const levelKey: Record<SkillLevel, string> = {
  [SkillLevel.beginner]: 'beginner',
  [SkillLevel.intermediate]: 'intermediate',
  [SkillLevel.advanced]: 'advanced',
}

export const goalKey: Record<MusicianGoal, string> = {
  [MusicianGoal.original]: 'original',
  [MusicianGoal.covers]: 'covers',
  [MusicianGoal.either]: 'either',
}

export const commitmentKey: Record<MusicianCommitment, string> = {
  [MusicianCommitment.justRehearse]: 'justRehearse',
  [MusicianCommitment.playLive]: 'playLive',
  [MusicianCommitment.record]: 'record',
}

export const reachKey: Record<Reach, string> = {
  [Reach.myCity]: 'myCity',
  [Reach.abc]: 'abc',
  [Reach.greaterSaoPaulo]: 'greaterSaoPaulo',
  [Reach.anywhere]: 'anywhere',
}

export const weekDayKey: Record<WeekDay, string> = {
  [WeekDay.monday]: 'monday',
  [WeekDay.tuesday]: 'tuesday',
  [WeekDay.wednesday]: 'wednesday',
  [WeekDay.thursday]: 'thursday',
  [WeekDay.friday]: 'friday',
  [WeekDay.saturday]: 'saturday',
  [WeekDay.sunday]: 'sunday',
}

export const dayPeriodKey: Record<DayPeriod, string> = {
  [DayPeriod.morning]: 'morning',
  [DayPeriod.afternoon]: 'afternoon',
  [DayPeriod.night]: 'night',
}

export const openingStatusKey: Record<OpeningStatus, string> = {
  [OpeningStatus.open]: 'open',
  [OpeningStatus.filled]: 'filled',
  [OpeningStatus.closed]: 'closed',
}

export const interestStatusKey: Record<InterestStatus, string> = {
  [InterestStatus.pending]: 'pending',
  [InterestStatus.invited]: 'invited',
  [InterestStatus.dismissed]: 'dismissed',
}

export const allInstruments = Object.values(Instrument) as Instrument[]
export const chipInstruments: Instrument[] = [Instrument.vocal, Instrument.guitar, Instrument.bass, Instrument.drums, Instrument.keys]
export const allWeekDays = Object.values(WeekDay) as WeekDay[]
export const allDayPeriods = Object.values(DayPeriod) as DayPeriod[]

export function useInstrumentLabel() {
  const { t } = useTranslation('ensaio')
  return (instrument: Instrument, otherName?: string | null) =>
    instrument === Instrument.other && otherName ? otherName : t(`instruments.${instrumentKey[instrument]}`)
}
